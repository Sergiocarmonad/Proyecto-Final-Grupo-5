using Game.Reef;
using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// Coordinates Level 4 octopus appearances, inter-spawn pauses, and obligation pausing via LevelFlowController.
    /// </summary>
    public sealed class OctopusFlowDriver : MonoBehaviour
    {
        [Header("Core Flow")]
        [SerializeField] private LevelFlowController _levelFlowController;
        [SerializeField] private OctopusEnemy _octopusEnemy;
        [SerializeField] private ReefHealth _reefHealth;
        [SerializeField] private ScreenInkController _screenInk;

        [Header("Paused Spawners During Combat")]
        [SerializeField] private TrashSpawner _trashSpawner;
        [SerializeField] private OilSpillSpawner _oilSpillSpawner;
        [SerializeField] private FishSpawner _fishSpawner;

        private bool _hasStartedForCurrentRun;
        private bool _isActiveForLevel;
        private float _spawnTimer;

        private void OnEnable()
        {
            if (_levelFlowController != null)
            {
                _levelFlowController.LevelStarted += HandleLevelStarted;
                _levelFlowController.LevelWon += HandleLevelEnded;
                _levelFlowController.LevelLost += HandleLevelEnded;
            }

            if (_octopusEnemy != null)
            {
                _octopusEnemy.OctopusAppeared += HandleOctopusAppeared;
                _octopusEnemy.OctopusRetreated += HandleOctopusRetreated;
            }
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
            if (!_isActiveForLevel
                || _levelFlowController == null
                || !_levelFlowController.IsRunning
                || _octopusEnemy == null
                || _octopusEnemy.IsPresent)
            {
                return;
            }

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                _octopusEnemy.Appear();
            }
        }

        private void OnDisable()
        {
            if (_levelFlowController != null)
            {
                _levelFlowController.LevelStarted -= HandleLevelStarted;
                _levelFlowController.LevelWon -= HandleLevelEnded;
                _levelFlowController.LevelLost -= HandleLevelEnded;
            }

            if (_octopusEnemy != null)
            {
                _octopusEnemy.OctopusAppeared -= HandleOctopusAppeared;
                _octopusEnemy.OctopusRetreated -= HandleOctopusRetreated;
            }

            _isActiveForLevel = false;
            SetObligationsPaused(false);
        }

        private void HandleLevelStarted(LevelConfig config)
        {
            _hasStartedForCurrentRun = true;
            SetObligationsPaused(false);

            if (_screenInk != null)
            {
                _screenInk.ResetInk();
            }

            if (_octopusEnemy != null)
            {
                _octopusEnemy.ResetState();
            }

            if (config == null || !config.OctopusEnabled || _octopusEnemy == null)
            {
                _isActiveForLevel = false;
                return;
            }

            _octopusEnemy.Configure(
                _reefHealth,
                config.OctopusMaxHits,
                config.InkSpawnInterval,
                config.InkBallSpeed,
                config.OctopusLaughDelay);

            _spawnTimer = config.InitialOctopusDelay;
            _isActiveForLevel = true;
        }

        private void HandleLevelEnded(LevelConfig _)
        {
            _hasStartedForCurrentRun = false;
            _isActiveForLevel = false;

            if (_octopusEnemy != null)
            {
                if (_octopusEnemy.IsPresent)
                {
                    _octopusEnemy.Retreat(OctopusRetreatReason.LevelEnded);
                }
                else
                {
                    _octopusEnemy.ResetState();
                }
            }

            SetObligationsPaused(false);
        }

        private void HandleOctopusAppeared()
        {
            SetObligationsPaused(true);
        }

        private void HandleOctopusRetreated(OctopusRetreatReason _)
        {
            SetObligationsPaused(false);

            if (_isActiveForLevel && _levelFlowController != null && _levelFlowController.CurrentLevel != null)
            {
                _spawnTimer = _levelFlowController.CurrentLevel.OctopusPauseDuration;
            }
        }

        private void SetObligationsPaused(bool paused)
        {
            if (_reefHealth != null)
            {
                _reefHealth.SetContinuousDamagePaused(paused);
            }

            if (_trashSpawner != null)
            {
                _trashSpawner.SetPaused(paused);
            }

            if (_oilSpillSpawner != null)
            {
                _oilSpillSpawner.SetPaused(paused);
            }

            if (_fishSpawner != null)
            {
                _fishSpawner.SetPaused(paused);
            }
        }
    }
}
