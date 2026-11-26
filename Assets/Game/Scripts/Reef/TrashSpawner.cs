using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Reef
{
    public readonly struct TrashSpawnOption
    {
        public TrashSpawnOption(TrashTypeConfig trashType, float weight)
        {
            TrashType = trashType;
            Weight = Mathf.Max(0f, weight);
        }

        public TrashTypeConfig TrashType { get; }
        public float Weight { get; }
    }

    /// <summary>
    /// Spawns pooled falling trash around the player and emits lifecycle and collection events.
    /// </summary>
    public sealed class TrashSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ReefHealth _reefHealth;
        [SerializeField] private Transform _spawnCenter;

        [Header("Spawn Geometry")]
        [SerializeField] private float _minSpawnRadius = 4f;
        [SerializeField] private float _maxSpawnRadius = 10f;
        [SerializeField] private float _spawnHeight = 10f;
        [SerializeField] private LayerMask _floorMask = ~0;
        [SerializeField] private float _fallbackFloorY;

        private readonly List<TrashSpawnOption> _options = new List<TrashSpawnOption>();
        private readonly List<TrashItem> _activeItems = new List<TrashItem>();
        private readonly Dictionary<TrashItem, Stack<TrashItem>> _poolByPrefab = new Dictionary<TrashItem, Stack<TrashItem>>();
        private readonly Dictionary<TrashItem, TrashItem> _prefabByInstance = new Dictionary<TrashItem, TrashItem>();

        private float _intervalStart = 4f;
        private float _intervalEnd = 2f;
        private int _maxActiveTrash;
        private float _normalizedProgress;
        private float _spawnTimer;
        private bool _isSpawning;
        private bool _isPaused;

        /// <summary>
        /// Raised when a trash item spawns above the player.
        /// </summary>
        public event Action<TrashItem> TrashSpawned;

        /// <summary>
        /// Raised when a trash item touches the seabed, passing (item, oneTimePenalty).
        /// </summary>
        public event Action<TrashItem, float> TrashLanded;

        /// <summary>
        /// Raised when a trash item is collected, passing (item, totalCollectedCount).
        /// </summary>
        public event Action<TrashItem, int> TrashCollected;

        /// <summary>
        /// Raised whenever CollectedCount changes.
        /// </summary>
        public event Action<int> CollectedCountChanged;

        public int CollectedCount { get; private set; }
        public int ActiveCount => _activeItems.Count;
        public bool IsSpawning => _isSpawning;
        public bool IsPaused => _isPaused;

        private void Update()
        {
            if (!_isSpawning || _isPaused || _options.Count == 0)
            {
                return;
            }

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer > 0f)
            {
                return;
            }

            if (_maxActiveTrash > 0 && _activeItems.Count >= _maxActiveTrash)
            {
                return;
            }

            SpawnRandomTrash();
            _spawnTimer = GetCurrentInterval();
        }

        private void OnDestroy()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                UnsubscribeItem(_activeItems[i]);
            }

            _activeItems.Clear();
        }

        /// <summary>
        /// Starts spawning trash using the provided weighted types and cadence settings.
        /// </summary>
        public void StartSpawning(
            IReadOnlyList<TrashSpawnOption> options,
            float initialDelay,
            float intervalStart,
            float intervalEnd,
            int maxActiveTrash)
        {
            ReturnAllActiveToPool();

            _options.Clear();
            if (options != null)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    TrashSpawnOption option = options[i];
                    if (option.TrashType != null && option.TrashType.Prefab != null)
                    {
                        _options.Add(option);
                    }
                }
            }

            _intervalStart = Mathf.Max(0.1f, intervalStart);
            _intervalEnd = Mathf.Max(0.1f, intervalEnd);
            _maxActiveTrash = Mathf.Max(0, maxActiveTrash);
            _normalizedProgress = 0f;
            _spawnTimer = Mathf.Max(0f, initialDelay);
            _isSpawning = _options.Count > 0;

            SetCollectedCount(0);
        }

        /// <summary>
        /// Updates normalized level progress [0..1] to interpolate spawn cadence.
        /// </summary>
        public void SetNormalizedProgress(float normalizedProgress)
        {
            _normalizedProgress = Mathf.Clamp01(normalizedProgress);
        }

        /// <summary>
        /// Pauses or resumes new trash spawns without clearing active items or resetting the spawn timer.
        /// </summary>
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        /// <summary>
        /// Stops spawning new trash and optionally returns active items to the pool.
        /// </summary>
        public void StopSpawning(bool clearActive)
        {
            _isSpawning = false;
            _isPaused = false;

            if (clearActive)
            {
                ReturnAllActiveToPool();
            }
        }

        private void SpawnRandomTrash()
        {
            TrashTypeConfig selectedType = SelectTrashType();
            if (selectedType == null || selectedType.Prefab == null)
            {
                return;
            }

            TrashItem instance = AcquireFromPool(selectedType.Prefab);
            instance.transform.SetPositionAndRotation(SampleSpawnPosition(), Quaternion.identity);
            instance.InitializeTrash(_reefHealth, selectedType, _floorMask, _fallbackFloorY);
            SubscribeItem(instance);
            _activeItems.Add(instance);
            instance.gameObject.SetActive(true);

            TrashSpawned?.Invoke(instance);
        }

        private TrashTypeConfig SelectTrashType()
        {
            float totalWeight = 0f;
            for (int i = 0; i < _options.Count; i++)
            {
                totalWeight += _options[i].Weight;
            }

            if (totalWeight <= 0f)
            {
                return _options.Count > 0 ? _options[0].TrashType : null;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < _options.Count; i++)
            {
                cumulative += _options[i].Weight;
                if (roll <= cumulative)
                {
                    return _options[i].TrashType;
                }
            }

            return _options[_options.Count - 1].TrashType;
        }

        private Vector3 SampleSpawnPosition()
        {
            Vector3 center = _spawnCenter != null ? _spawnCenter.position : transform.position;
            float minRadius = Mathf.Max(0f, Mathf.Min(_minSpawnRadius, _maxSpawnRadius));
            float maxRadius = Mathf.Max(minRadius, _maxSpawnRadius);
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float radius = UnityEngine.Random.Range(minRadius, maxRadius);

            return new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                center.y + _spawnHeight,
                center.z + Mathf.Sin(angle) * radius);
        }

        private float GetCurrentInterval()
        {
            return Mathf.Lerp(_intervalStart, _intervalEnd, _normalizedProgress);
        }

        private TrashItem AcquireFromPool(TrashItem prefab)
        {
            if (_poolByPrefab.TryGetValue(prefab, out Stack<TrashItem> pool))
            {
                while (pool.Count > 0)
                {
                    TrashItem pooled = pool.Pop();
                    if (pooled != null)
                    {
                        return pooled;
                    }
                }
            }

            TrashItem created = Instantiate(prefab, transform);
            created.gameObject.SetActive(false);
            _prefabByInstance[created] = prefab;
            return created;
        }

        private void ReturnToPool(TrashItem instance)
        {
            if (instance == null)
            {
                return;
            }

            UnsubscribeItem(instance);
            _activeItems.Remove(instance);
            instance.ResetState();
            instance.gameObject.SetActive(false);

            if (!_prefabByInstance.TryGetValue(instance, out TrashItem prefab) || prefab == null)
            {
                return;
            }

            if (!_poolByPrefab.TryGetValue(prefab, out Stack<TrashItem> pool))
            {
                pool = new Stack<TrashItem>();
                _poolByPrefab[prefab] = pool;
            }

            pool.Push(instance);
        }

        private void ReturnAllActiveToPool()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                ReturnToPool(_activeItems[i]);
            }
        }

        private void SubscribeItem(TrashItem item)
        {
            item.Landed += HandleItemLanded;
            item.Collected += HandleItemCollected;
        }

        private void UnsubscribeItem(TrashItem item)
        {
            if (item == null)
            {
                return;
            }

            item.Landed -= HandleItemLanded;
            item.Collected -= HandleItemCollected;
        }

        private void HandleItemLanded(TrashItem item, float penalty)
        {
            TrashLanded?.Invoke(item, penalty);
        }

        private void HandleItemCollected(TrashItem item)
        {
            ReturnToPool(item);
            SetCollectedCount(CollectedCount + 1);
            TrashCollected?.Invoke(item, CollectedCount);
        }

        private void SetCollectedCount(int count)
        {
            CollectedCount = count;
            CollectedCountChanged?.Invoke(CollectedCount);
        }
    }
}
