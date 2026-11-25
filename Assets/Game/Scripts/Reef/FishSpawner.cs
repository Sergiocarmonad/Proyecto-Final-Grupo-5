using System;
using System.Collections.Generic;
using Game.Radar;
using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Spawns and pools harmful fish inside a configured swim area up to a maximum active count.
    /// </summary>
    public sealed class FishSpawner : MonoBehaviour
    {
        [SerializeField] private HarmfulFish _fishPrefab;
        [SerializeField] private FishConfig _defaultFishConfig;
        [SerializeField] private ReefHealth _reefHealth;
        [SerializeField] private TrackableRegistry _trackableRegistry;

        [Header("Swim Area")]
        [SerializeField] private Transform _swimCenter;
        [SerializeField, Min(0.5f)] private float _swimRadius = 10f;

        [Header("Spawning")]
        [SerializeField, Min(0)] private int _maxActiveFish = 3;

        private readonly Stack<HarmfulFish> _pool = new Stack<HarmfulFish>();
        private readonly List<HarmfulFish> _activeFish = new List<HarmfulFish>();

        private bool _isSpawning;
        private float _spawnTimer;
        private float _intervalStart = 6f;
        private float _intervalEnd = 3f;
        private float _normalizedProgress;
        private int _effectiveMaxActiveFish;
        private FishConfig _activeFishConfig;

        /// <summary>
        /// Raised when a harmful fish spawns into the swim area.
        /// </summary>
        public event Action<HarmfulFish> FishSpawned;

        /// <summary>
        /// Raised when a spawned harmful fish is caught.
        /// </summary>
        public event Action<HarmfulFish> FishCaught;

        public bool IsSpawning => _isSpawning;
        public int ActiveFishCount => _activeFish.Count;

        private void Awake()
        {
            _effectiveMaxActiveFish = Mathf.Max(0, _maxActiveFish);
            _activeFishConfig = _defaultFishConfig;
        }

        private void Update()
        {
            if (!_isSpawning || _fishPrefab == null || _activeFishConfig == null)
            {
                return;
            }

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer > 0f)
            {
                return;
            }

            TrySpawnFish();
            float currentInterval = Mathf.Lerp(_intervalStart, _intervalEnd, _normalizedProgress);
            _spawnTimer = Mathf.Max(0.1f, currentInterval);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _activeFish.Count; i++)
            {
                if (_activeFish[i] != null)
                {
                    _activeFish[i].Caught -= HandleFishCaught;
                }
            }

            _activeFish.Clear();
            _pool.Clear();
        }

        /// <summary>
        /// Updates normalized level progress [0, 1] used to interpolate spawn intervals.
        /// </summary>
        public void SetNormalizedProgress(float normalizedProgress)
        {
            _normalizedProgress = Mathf.Clamp01(normalizedProgress);
        }

        /// <summary>
        /// Configures spawn timing and optional limits/config overrides, then starts spawning.
        /// </summary>
        public void ConfigureAndStart(
            float initialDelay,
            float intervalStart,
            float intervalEnd,
            int maxActiveFishOverride = -1,
            FishConfig fishConfigOverride = null)
        {
            _intervalStart = Mathf.Max(0.1f, intervalStart);
            _intervalEnd = Mathf.Max(0.1f, intervalEnd);
            _spawnTimer = Mathf.Max(0f, initialDelay);
            _normalizedProgress = 0f;
            _effectiveMaxActiveFish = maxActiveFishOverride >= 0
                ? maxActiveFishOverride
                : Mathf.Max(0, _maxActiveFish);
            _activeFishConfig = fishConfigOverride != null ? fishConfigOverride : _defaultFishConfig;
            _isSpawning = true;
        }

        /// <summary>
        /// Stops spawning and optionally returns all active fish to the pool.
        /// </summary>
        public void StopSpawning(bool clearActiveFish = false)
        {
            _isSpawning = false;

            if (!clearActiveFish)
            {
                return;
            }

            for (int i = _activeFish.Count - 1; i >= 0; i--)
            {
                ReturnFishToPool(_activeFish[i]);
            }

            _activeFish.Clear();
        }

        private void TrySpawnFish()
        {
            if (_activeFish.Count >= _effectiveMaxActiveFish)
            {
                return;
            }

            FishConfig fishConfig = _activeFishConfig;
            if (fishConfig == null || fishConfig.Obligation == null)
            {
                return;
            }

            HarmfulFish fish = GetOrCreateFish();
            if (fish == null)
            {
                return;
            }

            Vector3 center = GetSwimCenter();
            Vector3 spawnPosition = GetRandomSpawnPosition(center, fishConfig);
            Vector3 toCenter = center - spawnPosition;
            toCenter.y = 0f;
            Quaternion spawnRotation = toCenter.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toCenter.normalized, Vector3.up)
                : Quaternion.identity;

            fish.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            fish.ResetState();
            fish.SetTrackableRegistry(_trackableRegistry);
            fish.Configure(fishConfig, center, _swimRadius);
            fish.Initialize(_reefHealth, fishConfig.Obligation);
            fish.Caught += HandleFishCaught;

            _activeFish.Add(fish);
            fish.gameObject.SetActive(true);
            FishSpawned?.Invoke(fish);
        }

        private HarmfulFish GetOrCreateFish()
        {
            while (_pool.Count > 0)
            {
                HarmfulFish pooled = _pool.Pop();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            if (_fishPrefab == null)
            {
                return null;
            }

            HarmfulFish instance = Instantiate(_fishPrefab, transform);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void HandleFishCaught(HarmfulFish fish)
        {
            if (fish == null)
            {
                return;
            }

            _activeFish.Remove(fish);
            ReturnFishToPool(fish);
            FishCaught?.Invoke(fish);
        }

        private void ReturnFishToPool(HarmfulFish fish)
        {
            if (fish == null)
            {
                return;
            }

            fish.Caught -= HandleFishCaught;
            fish.ResetState();
            fish.gameObject.SetActive(false);
            _pool.Push(fish);
        }

        private Vector3 GetSwimCenter()
        {
            return _swimCenter != null ? _swimCenter.position : transform.position;
        }

        private Vector3 GetRandomSpawnPosition(Vector3 center, FishConfig fishConfig)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(UnityEngine.Random.value) * _swimRadius;
            float heightOffset = UnityEngine.Random.Range(fishConfig.MinHeightOffset, fishConfig.MaxHeightOffset);

            return center + new Vector3(
                Mathf.Cos(angle) * distance,
                heightOffset,
                Mathf.Sin(angle) * distance);
        }
    }
}
