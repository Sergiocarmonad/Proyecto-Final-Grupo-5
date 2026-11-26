using System;
using Game.Core;
using Game.Reef;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Flow
{
    public enum LevelOutcome
    {
        None = 0,
        Won = 1,
        Lost = 2
    }

    public enum GameState
    {
        None = 0,
        Playing = 1,
        Victory = 2,
        Defeat = 3
    }

    /// <summary>
    /// Summary of level completion metrics and bonus score emitted when a level is won.
    /// </summary>
    public readonly struct LevelResult
    {
        public LevelResult(
            int trashCollected,
            int totalTrash,
            float healthRemaining,
            float healthMissing,
            float healthMissingNormalized,
            int bonusPoints)
        {
            TrashCollected = trashCollected;
            TotalTrash = totalTrash;
            HealthRemaining = healthRemaining;
            HealthMissing = healthMissing;
            HealthMissingNormalized = healthMissingNormalized;
            BonusPoints = bonusPoints;
        }

        public int TrashCollected { get; }
        public int TotalTrash { get; }
        public float HealthRemaining { get; }
        public float HealthMissing { get; }
        public float HealthMissingNormalized { get; }
        public int BonusPoints { get; }
    }

    /// <summary>
    /// Drives the survival timer, game state transitions, and win/loss resolution for a single level configuration.
    /// </summary>
    public sealed class LevelFlowController : MonoBehaviour
    {
        [SerializeField] private LevelConfig[] _levelConfigs;
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private ReefHealth _reefHealth;
        [SerializeField] private TrashSpawner _trashSpawner;
        [SerializeField] private bool _autoStartOnStart = true;

        [Header("Scene Transitions")]
        [SerializeField] private string _gameplaySceneName = "Gameplay";
        [SerializeField] private string _menuSceneName;

        [Header("Editor Debug")]
        [SerializeField, Min(0)] private int _debugStartLevel;

        /// <summary>
        /// Raised when a level starts with its active configuration.
        /// </summary>
        public event Action<LevelConfig> LevelStarted;

        /// <summary>
        /// Raised when the survival timer finishes with reef health above zero.
        /// </summary>
        public event Action<LevelConfig> LevelWon;

        /// <summary>
        /// Raised after LevelWon when the game enters Victory state with final level metrics.
        /// </summary>
        public event Action<LevelResult> VictorySequenceStarted;

        /// <summary>
        /// Raised when reef health reaches zero before the survival timer finishes.
        /// </summary>
        public event Action<LevelConfig> LevelLost;

        /// <summary>
        /// Raised when the remaining survival time initializes, crosses a whole second, or finishes, passing (remainingSeconds, durationSeconds).
        /// </summary>
        public event Action<float, float> TimeRemainingChanged;

        public LevelConfig CurrentLevel { get; private set; }
        public float Duration { get; private set; }
        public float RemainingTime { get; private set; }
        public float NormalizedTimeRemaining => Duration > 0f ? RemainingTime / Duration : 0f;
        public bool IsRunning { get; private set; }
        public GameState State { get; private set; }
        public LevelOutcome Outcome { get; private set; }

        private int _lastAnnouncedWholeSecond = -1;
        private bool _isTransitioning;

        private void Start()
        {
            if (_autoStartOnStart)
            {
                StartLevel();
            }
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);

            if (RemainingTime <= 0f && !_reefHealth.IsDepleted)
            {
                CompleteLevel(LevelOutcome.Won);
                return;
            }

            int currentWholeSecond = Mathf.CeilToInt(RemainingTime);
            if (currentWholeSecond != _lastAnnouncedWholeSecond)
            {
                _lastAnnouncedWholeSecond = currentWholeSecond;
                TimeRemainingChanged?.Invoke(RemainingTime, Duration);
            }
        }

        private void OnDestroy()
        {
            if (_reefHealth != null)
            {
                _reefHealth.Depleted -= HandleReefDepleted;
            }
        }

        /// <summary>
        /// Initializes health and timer from the provided config (or LevelProgress selection) and starts the level.
        /// </summary>
        public void StartLevel(LevelConfig config = null)
        {
            LevelConfig targetConfig = ResolveTargetConfig(config);
            if (targetConfig == null || _reefHealth == null)
            {
                Debug.LogError("LevelFlowController requires both a LevelConfig and a ReefHealth reference.", this);
                return;
            }

            _reefHealth.Depleted -= HandleReefDepleted;

            CurrentLevel = targetConfig;
            Duration = targetConfig.DurationSeconds;
            RemainingTime = Duration;
            Outcome = LevelOutcome.None;
            State = GameState.Playing;
            IsRunning = true;
            _isTransitioning = false;
            _lastAnnouncedWholeSecond = Mathf.CeilToInt(RemainingTime);

            _reefHealth.ResetHealth(targetConfig.MaxHealth);
            _reefHealth.Depleted += HandleReefDepleted;

            LevelStarted?.Invoke(CurrentLevel);
            TimeRemainingChanged?.Invoke(RemainingTime, Duration);
        }

        /// <summary>
        /// Advances LevelProgress and reloads the Gameplay scene, or returns to menu after the final level.
        /// </summary>
        public void ProceedToNextLevel()
        {
            if (State != GameState.Victory || _isTransitioning)
            {
                return;
            }

            int totalLevels = _levelConfigs != null && _levelConfigs.Length > 0 ? _levelConfigs.Length : 1;
            int nextLevelIndex = LevelProgress.CurrentLevelIndex + 1;
            if (nextLevelIndex > totalLevels)
            {
                ReturnToMenu();
                return;
            }

            LevelProgress.SetLevel(nextLevelIndex);
            ReloadGameplayScene();
        }

        /// <summary>
        /// Reloads the Gameplay scene at the current level index, valid only in Defeat state.
        /// </summary>
        public void RestartLevel()
        {
            if (State != GameState.Defeat || _isTransitioning)
            {
                return;
            }

            ReloadGameplayScene();
        }

        /// <summary>
        /// Resets level progression and loads the configured menu scene, falling back to build index 0 when unset.
        /// </summary>
        public void ReturnToMenu()
        {
            if (_isTransitioning)
            {
                return;
            }

            LevelProgress.Reset();

            if (!string.IsNullOrWhiteSpace(_menuSceneName))
            {
                TryLoadSceneByName(_menuSceneName);
                return;
            }

            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                TryLoadSceneByBuildIndex(0);
                return;
            }

            Debug.LogWarning("Cannot return to menu because no menu scene name is set and Build Settings has no scenes.", this);
        }

        private void ReloadGameplayScene()
        {
            if (!string.IsNullOrWhiteSpace(_gameplaySceneName) && Application.CanStreamedLevelBeLoaded(_gameplaySceneName))
            {
                TryLoadSceneByName(_gameplaySceneName);
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                TryLoadSceneByBuildIndex(activeScene.buildIndex);
                return;
            }

            if (!string.IsNullOrEmpty(activeScene.name))
            {
                TryLoadSceneByName(activeScene.name);
            }
        }

        private LevelConfig ResolveTargetConfig(LevelConfig explicitConfig)
        {
            if (explicitConfig != null)
            {
                return explicitConfig;
            }

#if UNITY_EDITOR
            if (_debugStartLevel > 0)
            {
                int maxLevel = _levelConfigs != null && _levelConfigs.Length > 0 ? _levelConfigs.Length : _debugStartLevel;
                LevelProgress.SetLevel(Mathf.Min(_debugStartLevel, maxLevel));
            }
#endif

            if (_levelConfigs != null && _levelConfigs.Length > 0)
            {
                int index = Mathf.Clamp(LevelProgress.CurrentLevelIndex - 1, 0, _levelConfigs.Length - 1);
                if (_levelConfigs[index] != null)
                {
                    return _levelConfigs[index];
                }
            }

            return _levelConfig;
        }

        private void HandleReefDepleted()
        {
            if (!IsRunning)
            {
                return;
            }

            CompleteLevel(LevelOutcome.Lost);
        }

        private void CompleteLevel(LevelOutcome outcome)
        {
            IsRunning = false;
            Outcome = outcome;
            State = outcome == LevelOutcome.Won ? GameState.Victory :
                    outcome == LevelOutcome.Lost ? GameState.Defeat :
                    GameState.None;
            _lastAnnouncedWholeSecond = Mathf.CeilToInt(RemainingTime);
            TimeRemainingChanged?.Invoke(RemainingTime, Duration);

            if (_reefHealth != null)
            {
                _reefHealth.SetDamageBlocked(true);
                _reefHealth.Depleted -= HandleReefDepleted;
            }

            if (outcome == LevelOutcome.Won)
            {
                LevelResult result = BuildLevelResult();
                LevelWon?.Invoke(CurrentLevel);
                VictorySequenceStarted?.Invoke(result);
            }
            else if (outcome == LevelOutcome.Lost)
            {
                LevelLost?.Invoke(CurrentLevel);
            }
        }

        private LevelResult BuildLevelResult()
        {
            int trashCollected = _trashSpawner != null ? Mathf.Max(0, _trashSpawner.CollectedCount) : 0;
            int totalTrash = _trashSpawner != null ? Mathf.Max(trashCollected, _trashSpawner.SpawnedCount) : 0;
            float healthRemaining = _reefHealth != null ? Mathf.Max(0f, _reefHealth.CurrentHealth) : 0f;
            float maxHealth = _reefHealth != null ? Mathf.Max(0f, _reefHealth.MaxHealth) : 0f;
            float healthMissing = Mathf.Max(0f, maxHealth - healthRemaining);
            float healthMissingNormalized = maxHealth > 0f ? Mathf.Clamp01(healthMissing / maxHealth) : 0f;

            float bonusPerHealth = CurrentLevel != null ? CurrentLevel.BonusPointsPerHealth : 0f;
            float bonusPerTrash = CurrentLevel != null ? CurrentLevel.BonusPointsPerTrash : 0f;
            int bonusPoints = Mathf.RoundToInt(healthRemaining * bonusPerHealth + trashCollected * bonusPerTrash);

            return new LevelResult(
                trashCollected,
                totalTrash,
                healthRemaining,
                healthMissing,
                healthMissingNormalized,
                bonusPoints);
        }

        private void TryLoadSceneByName(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' could not be loaded. Verify it is added to Build Settings.", this);
                return;
            }

            _isTransitioning = true;
            SceneManager.LoadScene(sceneName);
        }

        private void TryLoadSceneByBuildIndex(int buildIndex)
        {
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"Scene build index {buildIndex} is out of Build Settings range.", this);
                return;
            }

            _isTransitioning = true;
            SceneManager.LoadScene(buildIndex);
        }
    }
}
