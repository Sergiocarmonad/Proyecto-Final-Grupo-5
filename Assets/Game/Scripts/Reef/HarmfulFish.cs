using System;
using Game.Radar;
using Game.Weapons;
using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Harmful invasive fish that patrols a reef area, damages health while alive, and stops when caught.
    /// </summary>
    public sealed class HarmfulFish : ObligationBase, ICatchable, IRadarTrackable
    {
        [SerializeField] private FishConfig _fishConfig;
        [SerializeField] private TrackableRegistry _trackableRegistry;

        private Vector3 _swimCenter;
        private float _swimRadiusOverride = -1f;
        private Vector3 _currentWaypoint;
        private bool _hasSwimCenter;
        private bool _hasWaypoint;

        /// <summary>
        /// Raised when this fish is caught while active, passing itself.
        /// </summary>
        public event Action<HarmfulFish> Caught;

        /// <summary>
        /// Raised when radar-relevant state changes.
        /// </summary>
        public event Action<IRadarTrackable> TrackableStateChanged;

        public FishConfig FishConfig => _fishConfig;
        public bool CanCatch => State == ObligationState.Pending;

        public RadarEntityType EntityType => RadarEntityType.Enemy;
        public RadarSignalColor RadarColor => RadarSignalColor.Red;
        public float RadarSeverity => State == ObligationState.Pending ? 1f : 0f;
        public Vector3 WorldPosition => transform.position;
        public bool IsTrackableActive => State == ObligationState.Pending;

        private void OnEnable()
        {
            if (_trackableRegistry != null)
            {
                _trackableRegistry.Register(this);
            }
        }

        private void Start()
        {
            if (!_hasSwimCenter)
            {
                _swimCenter = transform.position;
                _hasSwimCenter = true;
            }

            if (State == ObligationState.Inactive)
            {
                ObligationConfig obligationConfig = Config != null ? Config : (_fishConfig != null ? _fishConfig.Obligation : null);
                if (obligationConfig != null)
                {
                    Initialize(ReefHealth, obligationConfig);
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            if (State != ObligationState.Pending || _fishConfig == null)
            {
                return;
            }

            UpdateSwimMovement(Time.deltaTime);
        }

        private void OnDisable()
        {
            if (_trackableRegistry != null)
            {
                _trackableRegistry.Unregister(this);
            }
        }

        /// <summary>
        /// Assigns the radar registry used on enable/disable.
        /// </summary>
        public void SetTrackableRegistry(TrackableRegistry registry)
        {
            if (ReferenceEquals(_trackableRegistry, registry))
            {
                return;
            }

            if (_trackableRegistry != null && isActiveAndEnabled)
            {
                _trackableRegistry.Unregister(this);
            }

            _trackableRegistry = registry;

            if (_trackableRegistry != null && isActiveAndEnabled)
            {
                _trackableRegistry.Register(this);
            }
        }

        /// <summary>
        /// Assigns the fish config and swim area used for patrolling.
        /// </summary>
        public void Configure(FishConfig fishConfig, Vector3 swimCenter, float swimRadius)
        {
            if (fishConfig != null)
            {
                _fishConfig = fishConfig;
            }

            _swimCenter = swimCenter;
            _hasSwimCenter = true;
            _swimRadiusOverride = swimRadius > 0f ? swimRadius : -1f;
            _hasWaypoint = false;
        }

        /// <summary>
        /// Resets obligation and patrol state so the fish can be reused from a pool.
        /// </summary>
        public void ResetState()
        {
            ResetObligationState();
            _hasWaypoint = false;
            TrackableStateChanged?.Invoke(this);
        }

        /// <summary>
        /// Catches this fish when alive (Pending), stopping reef damage and deactivating it.
        /// </summary>
        public bool TryCatch()
        {
            if (State != ObligationState.Pending)
            {
                return false;
            }

            Resolve();
            return true;
        }

        protected override void OnBecamePending()
        {
            if (!_hasSwimCenter)
            {
                _swimCenter = transform.position;
                _hasSwimCenter = true;
            }

            PickNextWaypoint();
            TrackableStateChanged?.Invoke(this);
        }

        protected override void OnResolved()
        {
            TrackableStateChanged?.Invoke(this);
            Caught?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void UpdateSwimMovement(float deltaTime)
        {
            if (!_hasWaypoint)
            {
                PickNextWaypoint();
            }

            Vector3 position = transform.position;
            Vector3 toWaypoint = _currentWaypoint - position;
            float reachDistance = _fishConfig.WaypointReachDistance;

            if (toWaypoint.sqrMagnitude <= reachDistance * reachDistance)
            {
                PickNextWaypoint();
                return;
            }

            Vector3 direction = toWaypoint.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _fishConfig.TurnSpeed * deltaTime);
            transform.position = Vector3.MoveTowards(position, _currentWaypoint, _fishConfig.SwimSpeed * deltaTime);
        }

        private void PickNextWaypoint()
        {
            float radius = _swimRadiusOverride > 0f
                ? _swimRadiusOverride
                : (_fishConfig != null ? _fishConfig.PatrolRadius : 5f);

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(UnityEngine.Random.value) * radius;
            float minHeight = _fishConfig != null ? _fishConfig.MinHeightOffset : -1f;
            float maxHeight = _fishConfig != null ? _fishConfig.MaxHeightOffset : 1f;
            float heightOffset = UnityEngine.Random.Range(minHeight, maxHeight);

            _currentWaypoint = _swimCenter + new Vector3(
                Mathf.Cos(angle) * distance,
                heightOffset,
                Mathf.Sin(angle) * distance);
            _hasWaypoint = true;
        }
    }
}
