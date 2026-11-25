using System;
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
    public sealed class CoralZone : ObligationBase
    {
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

        public string ZoneId => _zoneId;
        public float Dirtiness => _dirtiness;
        public bool IsClean => State != ObligationState.Pending;

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
            _dirtiness = Mathf.Clamp01(_dirtiness + clampedIntensity);

            if (wasClean)
            {
                BeginPending();
                CoralDirtyingStarted?.Invoke(this);
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
            }

            return true;
        }

        /// <summary>
        /// Resets the zone back to a clean, inactive state without emitting gameplay events.
        /// </summary>
        public void ResetZone()
        {
            _dirtiness = 0f;
            _hasCleaningStarted = false;
            ResetObligationState();
        }

        protected override void OnBecamePending()
        {
            _hasCleaningStarted = false;
        }
    }
}
