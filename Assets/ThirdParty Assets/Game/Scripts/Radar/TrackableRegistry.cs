using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Radar
{
    /// <summary>
    /// Central runtime registry of radar-trackable entities with registry-assigned integer IDs.
    /// </summary>
    public sealed class TrackableRegistry : MonoBehaviour
    {
        private readonly List<TrackableRecord> _entries = new List<TrackableRecord>();
        private readonly Dictionary<IRadarTrackable, int> _idByTrackable = new Dictionary<IRadarTrackable, int>();
        private int _nextTrackableId = 1;

        public event Action<int, IRadarTrackable> EntityRegistered;
        public event Action<int, IRadarTrackable> EntityUnregistered;
        public event Action<int, IRadarTrackable> EntityStateChanged;

        public IReadOnlyList<TrackableRecord> Entries => _entries;

        private void OnDestroy()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                IRadarTrackable trackable = _entries[i].Trackable;
                if (trackable != null)
                {
                    trackable.TrackableStateChanged -= HandleTrackableStateChanged;
                }
            }

            _entries.Clear();
            _idByTrackable.Clear();
        }

        /// <summary>
        /// Registers a trackable entity and returns its assigned positive ID (0 if null).
        /// </summary>
        public int Register(IRadarTrackable trackable)
        {
            if (trackable == null)
            {
                return 0;
            }

            if (_idByTrackable.TryGetValue(trackable, out int existingId))
            {
                return existingId;
            }

            int assignedId = _nextTrackableId++;
            _idByTrackable.Add(trackable, assignedId);
            _entries.Add(new TrackableRecord(assignedId, trackable));
            trackable.TrackableStateChanged += HandleTrackableStateChanged;

            EntityRegistered?.Invoke(assignedId, trackable);
            return assignedId;
        }

        /// <summary>
        /// Unregisters a previously registered entity and stops listening to its state changes.
        /// </summary>
        public bool Unregister(IRadarTrackable trackable)
        {
            if (trackable == null || !_idByTrackable.TryGetValue(trackable, out int assignedId))
            {
                return false;
            }

            trackable.TrackableStateChanged -= HandleTrackableStateChanged;
            _idByTrackable.Remove(trackable);

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].TrackableId == assignedId)
                {
                    _entries.RemoveAt(i);
                    break;
                }
            }

            EntityUnregistered?.Invoke(assignedId, trackable);
            return true;
        }

        public bool TryGetTrackableId(IRadarTrackable trackable, out int trackableId)
        {
            if (trackable == null)
            {
                trackableId = 0;
                return false;
            }

            return _idByTrackable.TryGetValue(trackable, out trackableId);
        }

        private void HandleTrackableStateChanged(IRadarTrackable trackable)
        {
            if (trackable == null || !_idByTrackable.TryGetValue(trackable, out int assignedId))
            {
                return;
            }

            EntityStateChanged?.Invoke(assignedId, trackable);
        }
    }
}
