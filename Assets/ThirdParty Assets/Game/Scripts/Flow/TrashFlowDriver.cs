using System.Collections.Generic;
using Game.Reef;
using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// Connects LevelFlowController lifecycle and LevelConfig trash settings to TrashSpawner.
    /// </summary>
    public sealed class TrashFlowDriver : MonoBehaviour
    {
        [SerializeField] private LevelFlowController _levelFlowController;
        [SerializeField] private TrashSpawner _trashSpawner;
        [SerializeField] private bool _clearTrashOnLevelEnd = true;

        private readonly List<TrashSpawnOption> _spawnOptionsBuffer = new List<TrashSpawnOption>();
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
            if (_levelFlowController == null || _trashSpawner == null || !_levelFlowController.IsRunning)
            {
                return;
            }

            _trashSpawner.SetNormalizedProgress(1f - _levelFlowController.NormalizedTimeRemaining);
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

            if (_trashSpawner == null)
            {
                return;
            }

            if (config == null || !config.TrashEnabled)
            {
                _trashSpawner.StopSpawning(true);
                return;
            }

            _spawnOptionsBuffer.Clear();
            IReadOnlyList<TrashSpawnEntry> entries = config.TrashTypes;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    TrashSpawnEntry entry = entries[i];
                    if (entry != null && entry.TrashType != null)
                    {
                        _spawnOptionsBuffer.Add(new TrashSpawnOption(entry.TrashType, entry.Weight));
                    }
                }
            }

            _trashSpawner.StartSpawning(
                _spawnOptionsBuffer,
                config.InitialTrashDelay,
                config.TrashSpawnIntervalStart,
                config.TrashSpawnIntervalEnd,
                config.MaxActiveTrash);
        }

        private void HandleLevelEnded(LevelConfig _)
        {
            _hasStartedForCurrentRun = false;

            if (_trashSpawner != null)
            {
                _trashSpawner.StopSpawning(_clearTrashOnLevelEnd);
            }
        }
    }
}
