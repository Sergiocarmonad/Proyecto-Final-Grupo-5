using System;
using Game.Radar;
using UnityEngine;

namespace Game.Reef
{
    public enum CoralZoneState
    {
        Clean = 0,
        Dirtying = 1,
        Dirty = 2
    }

    /// <summary>
    /// Reef coral zone modeled as an obligation that drains reef health while dirtying or dirty.
    /// </summary>
    public sealed class CoralZone : ObligationBase, IRadarTrackable
    {
        [SerializeField] private TrackableRegistry _trackableRegistry;
        [SerializeField] private string _zoneId;
        [SerializeField, Range(0.01f, 1f)] private float _dirtyThreshold = 0.75f;
        [SerializeField, Min(0f)] private float _passiveDirtyRate = 0.05f;

        private float _dirtiness;
        private bool _hasCleaningStarted;

        /// <summary>
        /// Raised when a clean coral zone starts getting dirty, passing the affected zone.
        /// </summary>
        public event Action<CoralZone> CoralDirtyingStarted;

        /// <summary>
        /// Raised on the first cleaning input applied to a dirtying or dirty zone.
        /// </summary>
        public event Action<CoralZone> CleaningStarted;

        /// <summary>
        /// Raised when cleaning reduces dirtiness, passing remaining dirtiness normalized in [0, 1] (1 -> 0).
        /// </summary>
        public event Action<float> CleaningProgressed;

        /// <summary>
        /// Raised when the zone reaches zero dirtiness and returns to the clean state.
        /// </summary>
        public event Action<CoralZone> Cleaned;

        /// <inheritdoc />
        public event Action<IRadarTrackable> TrackableStateChanged;

        public string ZoneId => _zoneId;
        public float Dirtiness => _dirtiness;
        public bool IsClean => State != ObligationState.Pending;

        public RadarEntityType EntityType => RadarEntityType.CoralZone;
        public RadarSignalColor RadarColor => IsClean ? RadarSignalColor.Green : RadarSignalColor.Orange;
        public float RadarSeverity => _dirtiness;
        public Vector3 WorldPosition => transform.position;
        public bool IsTrackableActive => isActiveAndEnabled;

        public CoralZoneState ZoneState
        {
            get
            {
                if (State != ObligationState.Pending)
                {
                    return CoralZoneState.Clean;
                }

                return _dirtiness >= _dirtyThreshold ? CoralZoneState.Dirty : CoralZoneState.Dirtying;
            }
        }

        private void OnEnable()
        {
            if (_trackableRegistry != null)
            {
                _trackableRegistry.Register(this);
            }
        }

        private void OnDisable()
        {
            if (_trackableRegistry != null)
            {
                _trackableRegistry.Unregister(this);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (State != ObligationState.Pending || _passiveDirtyRate <= 0f || _dirtiness >= 1f)
            {
                return;
            }

            _dirtiness = Mathf.Min(1f, _dirtiness + _passiveDirtyRate * Time.deltaTime);
        }

        /// <summary>
        /// Applies dirtiness from any contamination source (oil spill or octopus ink).
        /// </summary>
        public void StartDirtying(float intensity)
        {
            float clampedIntensity = Mathf.Max(0f, intensity);
            if (clampedIntensity <= 0f)
            {
                return;
            }

            bool wasClean = State == ObligationState.Inactive;
            float previousDirtiness = _dirtiness;
            _dirtiness = Mathf.Clamp01(_dirtiness + clampedIntensity);

            if (wasClean)
            {
                BeginPending();
                CoralDirtyingStarted?.Invoke(this);
                TrackableStateChanged?.Invoke(this);
            }
            else if (!Mathf.Approximately(_dirtiness, previousDirtiness))
            {
                TrackableStateChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Reduces zone dirtiness by the given amount and resolves the obligation when completely clean.
        /// </summary>
        public bool ApplyCleaning(float amount)
        {
            if (State != ObligationState.Pending || amount <= 0f)
            {
                return false;
            }

            if (!_hasCleaningStarted)
            {
                _hasCleaningStarted = true;
                CleaningStarted?.Invoke(this);
            }

            _dirtiness = Mathf.Max(0f, _dirtiness - amount);
            CleaningProgressed?.Invoke(_dirtiness);

            if (_dirtiness <= 0f)
            {
                _dirtiness = 0f;
                _hasCleaningStarted = false;
                Resolve();
                Cleaned?.Invoke(this);
                ResetObligationState();
                TrackableStateChanged?.Invoke(this);
            }

            return true;
        }

        /// <summary>
        /// Resets the zone back to a clean, inactive state without emitting gameplay events.
        /// </summary>
        public void ResetZone()
        {
            bool hadDirtiness = !IsClean || _dirtiness > 0f;
            _dirtiness = 0f;
            _hasCleaningStarted = false;
            ResetObligationState();

            if (hadDirtiness)
            {
                TrackableStateChanged?.Invoke(this);
            }
        }

        protected override void OnBecamePending()
        {
            _hasCleaningStarted = false;
        }
    }
}
