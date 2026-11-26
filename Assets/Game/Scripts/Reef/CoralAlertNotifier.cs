using System;
using System.Collections.Generic;
using Game.Radar;
using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Aggregates CoralZone registration events from TrackableRegistry and emits player-facing coral alerts.
    /// </summary>
    public sealed class CoralAlertNotifier : MonoBehaviour
    {
        [SerializeField] private TrackableRegistry _trackableRegistry;

        private readonly HashSet<CoralZone> _trackedZones = new HashSet<CoralZone>();
        private readonly HashSet<CoralZone> _activeAlerts = new HashSet<CoralZone>();

        public event Action<CoralZone> AlertRaised;
        public event Action<CoralZone> AlertCleared;

        public int ActiveAlertCount => _activeAlerts.Count;
        public bool HasActiveAlert => _activeAlerts.Count > 0;

        private void OnEnable()
        {
            if (_trackableRegistry == null)
            {
                return;
            }

            _trackableRegistry.EntityRegistered += HandleEntityRegistered;
            _trackableRegistry.EntityUnregistered += HandleEntityUnregistered;

            IReadOnlyList<TrackableRecord> entries = _trackableRegistry.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                TrackZone(entries[i].Trackable);
            }
        }

        private void OnDisable()
        {
            if (_trackableRegistry != null)
            {
                _trackableRegistry.EntityRegistered -= HandleEntityRegistered;
                _trackableRegistry.EntityUnregistered -= HandleEntityUnregistered;
            }

            foreach (CoralZone zone in _trackedZones)
            {
                if (zone != null)
                {
                    zone.CoralDirtyingStarted -= HandleCoralDirtyingStarted;
                    zone.Cleaned -= HandleCoralCleaned;
                    zone.TrackableStateChanged -= HandleTrackableStateChanged;
                }
            }

            _trackedZones.Clear();
            _activeAlerts.Clear();
        }

        private void HandleEntityRegistered(int _, IRadarTrackable trackable)
        {
            TrackZone(trackable);
        }

        private void HandleEntityUnregistered(int _, IRadarTrackable trackable)
        {
            CoralZone zone = trackable as CoralZone;
            if (zone == null || !_trackedZones.Remove(zone))
            {
                return;
            }

            zone.CoralDirtyingStarted -= HandleCoralDirtyingStarted;
            zone.Cleaned -= HandleCoralCleaned;
            zone.TrackableStateChanged -= HandleTrackableStateChanged;

            if (_activeAlerts.Remove(zone))
            {
                AlertCleared?.Invoke(zone);
            }
        }

        private void TrackZone(IRadarTrackable trackable)
        {
            CoralZone zone = trackable as CoralZone;
            if (zone == null || !_trackedZones.Add(zone))
            {
                return;
            }

            zone.CoralDirtyingStarted += HandleCoralDirtyingStarted;
            zone.Cleaned += HandleCoralCleaned;
            zone.TrackableStateChanged += HandleTrackableStateChanged;
        }

        private void HandleCoralDirtyingStarted(CoralZone zone)
        {
            if (zone == null || !_activeAlerts.Add(zone))
            {
                return;
            }

            AlertRaised?.Invoke(zone);
        }

        private void HandleCoralCleaned(CoralZone zone)
        {
            if (zone == null || !_activeAlerts.Remove(zone))
            {
                return;
            }

            AlertCleared?.Invoke(zone);
        }

        private void HandleTrackableStateChanged(IRadarTrackable trackable)
        {
            CoralZone zone = trackable as CoralZone;
            if (zone != null && zone.IsClean && _activeAlerts.Remove(zone))
            {
                AlertCleared?.Invoke(zone);
            }
        }
    }
}
