using Game.Reef;
using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// Connects LevelFlowController lifecycle and LevelConfig oil spill settings to OilSpillSpawner.
    /// </summary>
    public sealed class OilSpillFlowDriver : MonoBehaviour
    {
        [SerializeField] private LevelFlowController _levelFlowController;
        [SerializeField] private OilSpillSpawner _oilSpillSpawner;
        [SerializeField] private bool _resetCoralZonesOnLevelEnd = true;

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
            if (_levelFlowController == null || _oilSpillSpawner == null || !_levelFlowController.IsRunning)
            {
                return;
            }

            _oilSpillSpawner.SetNormalizedProgress(1f - _levelFlowController.NormalizedTimeRemaining);
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

            if (_oilSpillSpawner == null)
            {
                return;
            }

            if (config == null || !config.OilSpillsEnabled)
            {
                _oilSpillSpawner.StopSpawning(true);
                return;
            }

            _oilSpillSpawner.ConfigureAndStart(
                config.InitialOilSpillDelay,
                config.OilSpillIntervalStart,
                config.OilSpillIntervalEnd,
                config.OilSpillIntensity);
        }

        private void HandleLevelEnded(LevelConfig _)
        {
            _hasStartedForCurrentRun = false;

            if (_oilSpillSpawner != null)
            {
                _oilSpillSpawner.StopSpawning(_resetCoralZonesOnLevelEnd);
            }
        }
    }
}
