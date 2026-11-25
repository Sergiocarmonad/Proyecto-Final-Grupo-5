using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Radar
{
    /// <summary>
    /// Reads TrackableRegistry entries and publishes spatial radar snapshots on state changes or a fixed interval.
    /// </summary>
    public sealed class RadarController : MonoBehaviour
    {
        private const float MinVectorSqrMagnitude = 0.0001f;

        [SerializeField] private TrackableRegistry _registry;
        [SerializeField] private Transform _radarOrigin;
        [SerializeField, Min(0.1f)] private float _radarRange = 50f;
        [SerializeField, Min(0.05f)] private float _refreshInterval = 0.2f;
        [SerializeField] private bool _includeInactiveEntities;

        private readonly List<RadarBlipData> _blips = new List<RadarBlipData>();
        private float _refreshTimer;

        public event Action<IReadOnlyList<RadarBlipData>> RadarUpdated;
        public event Action<RadarBlipData> BlipChanged;

        public float RadarRange => Mathf.Max(0.1f, _radarRange);
        public float OriginYawDegrees => _radarOrigin != null ? _radarOrigin.eulerAngles.y : 0f;
        public IReadOnlyList<RadarBlipData> CurrentBlips => _blips;

        private void OnEnable()
        {
            if (_registry == null)
            {
                return;
            }

            _registry.EntityRegistered += HandleRegistryMembershipChanged;
            _registry.EntityUnregistered += HandleRegistryMembershipChanged;
            _registry.EntityStateChanged += HandleEntityStateChanged;
            RefreshRadar();
        }

        private void Update()
        {
            if (_registry == null)
            {
                return;
            }

            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer > 0f)
            {
                return;
            }

            RefreshRadar();
        }

        private void OnDisable()
        {
            if (_registry == null)
            {
                return;
            }

            _registry.EntityRegistered -= HandleRegistryMembershipChanged;
            _registry.EntityUnregistered -= HandleRegistryMembershipChanged;
            _registry.EntityStateChanged -= HandleEntityStateChanged;
        }

        /// <summary>
        /// Rebuilds the radar snapshot from the registry and notifies listeners.
        /// </summary>
        public void RefreshRadar()
        {
            RebuildBlips();
            _refreshTimer = Mathf.Max(0.05f, _refreshInterval);
            RadarUpdated?.Invoke(_blips);
        }

        private void HandleRegistryMembershipChanged(int _, IRadarTrackable __)
        {
            RefreshRadar();
        }

        private void HandleEntityStateChanged(int trackableId, IRadarTrackable trackable)
        {
            if (trackable == null)
            {
                return;
            }

            RadarBlipData updatedBlip = CreateBlipData(trackableId, trackable);
            RebuildBlips();
            _refreshTimer = Mathf.Max(0.05f, _refreshInterval);

            if (_includeInactiveEntities || updatedBlip.IsActive)
            {
                BlipChanged?.Invoke(updatedBlip);
            }

            RadarUpdated?.Invoke(_blips);
        }

        private void RebuildBlips()
        {
            _blips.Clear();
            if (_registry == null)
            {
                return;
            }

            IReadOnlyList<TrackableRecord> entries = _registry.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                TrackableRecord record = entries[i];
                if (record.Trackable == null)
                {
                    continue;
                }

                if (!_includeInactiveEntities && !record.Trackable.IsTrackableActive)
                {
                    continue;
                }

                _blips.Add(CreateBlipData(record.TrackableId, record.Trackable));
            }
        }

        private RadarBlipData CreateBlipData(int trackableId, IRadarTrackable trackable)
        {
            Vector3 worldPosition = trackable.WorldPosition;
            Vector3 originPosition = _radarOrigin != null ? _radarOrigin.position : Vector3.zero;
            Vector3 horizontalOffset = new Vector3(
                worldPosition.x - originPosition.x,
                0f,
                worldPosition.z - originPosition.z);

            float normalizedDistance = Mathf.Clamp01(horizontalOffset.magnitude / RadarRange);
            float relativeAngleDegrees = ComputeRelativeAngleDegrees(horizontalOffset);

            return new RadarBlipData(
                trackableId,
                trackable.EntityType,
                trackable.RadarColor,
                trackable.RadarSeverity,
                worldPosition,
                relativeAngleDegrees,
                normalizedDistance,
                trackable.IsTrackableActive);
        }

        private float ComputeRelativeAngleDegrees(Vector3 horizontalOffset)
        {
            if (horizontalOffset.sqrMagnitude <= MinVectorSqrMagnitude)
            {
                return 0f;
            }

            Vector3 originForward = ResolveOriginHorizontalForward();
            return Vector3.SignedAngle(originForward, horizontalOffset, Vector3.up);
        }

        private Vector3 ResolveOriginHorizontalForward()
        {
            if (_radarOrigin == null)
            {
                return Vector3.forward;
            }

            Vector3 forward = _radarOrigin.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > MinVectorSqrMagnitude)
            {
                return forward.normalized;
            }

            return Quaternion.Euler(0f, _radarOrigin.eulerAngles.y, 0f) * Vector3.forward;
        }
    }
}
