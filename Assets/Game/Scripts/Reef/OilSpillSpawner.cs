using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Periodically triggers oil spills onto coral zones, prioritizing clean zones.
    /// </summary>
    public sealed class OilSpillSpawner : MonoBehaviour
    {
        [SerializeField] private CoralZone[] _coralZones;

        private readonly List<string> _allowedZoneIds = new List<string>();
        private readonly List<CoralZone> _cleanCandidates = new List<CoralZone>();
        private readonly List<CoralZone> _fallbackCandidates = new List<CoralZone>();

        private bool _isSpawning;
        private bool _isPaused;
        private float _spawnTimer;
        private float _intervalStart = 1f;
        private float _intervalEnd = 1f;
        private float _intensity = 0.25f;
        private float _normalizedProgress;

        /// <summary>
        /// Raised when an oil spill hits a coral zone, passing the targeted zone.
        /// </summary>
        public event Action<CoralZone> OilSpillStarted;

        public bool IsSpawning => _isSpawning;
        public bool IsPaused => _isPaused;
        public IReadOnlyList<CoralZone> CoralZones => _coralZones;

        private void Update()
        {
            if (!_isSpawning || _isPaused || _coralZones == null || _coralZones.Length == 0)
            {
                return;
            }

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer > 0f)
            {
                return;
            }

            TryTriggerSpill();
            float currentInterval = Mathf.Lerp(_intervalStart, _intervalEnd, _normalizedProgress);
            _spawnTimer = Mathf.Max(0.1f, currentInterval);
        }

        /// <summary>
        /// Updates normalized level progress [0, 1] used to interpolate between start and end spill intervals.
        /// </summary>
        public void SetNormalizedProgress(float normalizedProgress)
        {
            _normalizedProgress = Mathf.Clamp01(normalizedProgress);
        }

        /// <summary>
        /// Configures oil spill timings, intensity, and optional zone ID filter, then starts spawning.
        /// </summary>
        public void ConfigureAndStart(
            float initialDelay,
            float intervalStart,
            float intervalEnd,
            float intensity,
            IReadOnlyList<string> allowedZoneIds = null)
        {
            _intervalStart = Mathf.Max(0.1f, intervalStart);
            _intervalEnd = Mathf.Max(0.1f, intervalEnd);
            _intensity = Mathf.Max(0.01f, intensity);
            _spawnTimer = Mathf.Max(0f, initialDelay);
            _normalizedProgress = 0f;

            _allowedZoneIds.Clear();
            if (allowedZoneIds != null)
            {
                for (int i = 0; i < allowedZoneIds.Count; i++)
                {
                    string id = allowedZoneIds[i];
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _allowedZoneIds.Add(id);
                    }
                }
            }

            _isSpawning = true;
        }

        /// <summary>
        /// Pauses or resumes new oil spill scheduling without resetting zones or the spawn timer.
        /// </summary>
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        /// <summary>
        /// Stops scheduling oil spills and optionally resets all managed coral zones.
        /// </summary>
        public void StopSpawning(bool resetZones = false)
        {
            _isSpawning = false;
            _isPaused = false;
            _spawnTimer = 0f;

            if (!resetZones || _coralZones == null)
            {
                return;
            }

            for (int i = 0; i < _coralZones.Length; i++)
            {
                CoralZone zone = _coralZones[i];
                if (zone != null)
                {
                    zone.ResetZone();
                }
            }
        }

        private void TryTriggerSpill()
        {
            _cleanCandidates.Clear();
            _fallbackCandidates.Clear();

            for (int i = 0; i < _coralZones.Length; i++)
            {
                CoralZone zone = _coralZones[i];
                if (zone == null || !IsZoneAllowed(zone))
                {
                    continue;
                }

                if (zone.IsClean)
                {
                    _cleanCandidates.Add(zone);
                }
                else if (zone.Dirtiness < 1f)
                {
                    _fallbackCandidates.Add(zone);
                }
            }

            List<CoralZone> pool = _cleanCandidates.Count > 0 ? _cleanCandidates : _fallbackCandidates;
            if (pool.Count == 0)
            {
                return;
            }

            CoralZone target = pool[UnityEngine.Random.Range(0, pool.Count)];
            target.StartDirtying(_intensity);
            OilSpillStarted?.Invoke(target);
        }

        private bool IsZoneAllowed(CoralZone zone)
        {
            if (_allowedZoneIds.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < _allowedZoneIds.Count; i++)
            {
                if (string.Equals(zone.ZoneId, _allowedZoneIds[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
