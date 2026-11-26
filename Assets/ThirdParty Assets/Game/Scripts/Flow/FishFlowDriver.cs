using System.Collections.Generic;
using Game.Reef;
using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// Connects LevelFlowController lifecycle and LevelConfig fish settings to FishSpawner.
    /// </summary>
    public sealed class FishFlowDriver : MonoBehaviour
    {
        [SerializeField] private LevelFlowController _levelFlowController;
        [SerializeField] private FishSpawner _fishSpawner;
        [SerializeField] private bool _clearFishOnLevelEnd = true;

        private bool _hasStartedForCurrentRun;

        private void OnEnable()
        {
            if (_levelFlowController == null)
            {
                return;
            }

            _levelFlowController.LevelStarted += HandleLevelStarted;
            _levelFlowController.LevelWon += HandleLevelEnded;
            _levelFlowController.LevelLost += HandleLevelEnded;
        }

        private void Start()
        {
            if (!_hasStartedForCurrentRun
                && _levelFlowController != null
                && _levelFlowController.IsRunning
                && _levelFlowController.CurrentLevel != null)
            {
                HandleLevelStarted(_levelFlowController.CurrentLevel);
            }
        }

        private void Update()
        {
            if (_levelFlowController == null || _fishSpawner == null || !_levelFlowController.IsRunning)
            {
                return;
            }

            _fishSpawner.SetNormalizedProgress(1f - _levelFlowController.NormalizedTimeRemaining);
        }

        private void OnDisable()
        {
            if (_levelFlowController == null)
            {
                return;
            }

            _levelFlowController.LevelStarted -= HandleLevelStarted;
            _levelFlowController.LevelWon -= HandleLevelEnded;
            _levelFlowController.LevelLost -= HandleLevelEnded;
        }

        private void HandleLevelStarted(LevelConfig config)
        {
            _hasStartedForCurrentRun = true;

            if (_fishSpawner == null)
            {
                return;
            }

            if (config == null || !config.FishEnabled)
            {
                _fishSpawner.StopSpawning(true);
                return;
            }

            int maxActiveOverride = config.MaxActiveFish > 0 ? config.MaxActiveFish : -1;
            FishConfig selectedFishConfig = SelectFishConfig(config.FishTypes);

            _fishSpawner.ConfigureAndStart(
                config.InitialFishDelay,
                config.FishSpawnIntervalStart,
                config.FishSpawnIntervalEnd,
                maxActiveOverride,
                selectedFishConfig);
        }

        private void HandleLevelEnded(LevelConfig _)
        {
            _hasStartedForCurrentRun = false;

            if (_fishSpawner != null)
            {
                _fishSpawner.StopSpawning(_clearFishOnLevelEnd);
            }
        }

        private static FishConfig SelectFishConfig(IReadOnlyList<FishSpawnEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            FishConfig fallback = null;

            for (int i = 0; i < entries.Count; i++)
            {
                FishSpawnEntry entry = entries[i];
                if (entry == null || entry.FishConfig == null)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = entry.FishConfig;
                }

                totalWeight += entry.Weight;
            }

            if (totalWeight <= 0f)
            {
                return fallback;
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                FishSpawnEntry entry = entries[i];
                if (entry == null || entry.FishConfig == null || entry.Weight <= 0f)
                {
                    continue;
                }

                cumulative += entry.Weight;
                if (roll <= cumulative)
                {
                    return entry.FishConfig;
                }
            }

            return fallback;
        }
    }
}
