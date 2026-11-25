using System;
using Game.Reef;
using UnityEngine;

namespace Game.Flow
{
    public enum LevelOutcome
    {
        None = 0,
        Won = 1,
        Lost = 2
    }

    /// <summary>
    /// Drives the survival timer and win/loss resolution for a single level configuration.
    /// </summary>
    public sealed class LevelFlowController : MonoBehaviour
    {
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private ReefHealth _reefHealth;
        [SerializeField] private bool _autoStartOnStart = true;

        /// <summary>
        /// Raised when a level starts with its active configuration.
        /// </summary>
        public event Action<LevelConfig> LevelStarted;

        /// <summary>
        /// Raised when the survival timer finishes with reef health above zero.
        /// </summary>
        public event Action<LevelConfig> LevelWon;

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
        public LevelOutcome Outcome { get; private set; }

        private int _lastAnnouncedWholeSecond = -1;

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
        /// Initializes health and timer from the provided config (or the serialized default) and starts the level.
        /// </summary>
        public void StartLevel(LevelConfig config = null)
        {
            LevelConfig targetConfig = config != null ? config : _levelConfig;
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
            IsRunning = true;
            _lastAnnouncedWholeSecond = Mathf.CeilToInt(RemainingTime);

            _reefHealth.ResetHealth(targetConfig.MaxHealth);
            _reefHealth.Depleted += HandleReefDepleted;

            LevelStarted?.Invoke(CurrentLevel);
            TimeRemainingChanged?.Invoke(RemainingTime, Duration);
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
            _lastAnnouncedWholeSecond = Mathf.CeilToInt(RemainingTime);
            TimeRemainingChanged?.Invoke(RemainingTime, Duration);

            if (_reefHealth != null)
            {
                _reefHealth.Depleted -= HandleReefDepleted;
            }

            if (outcome == LevelOutcome.Won)
            {
                LevelWon?.Invoke(CurrentLevel);
            }
            else if (outcome == LevelOutcome.Lost)
            {
                LevelLost?.Invoke(CurrentLevel);
            }
        }
    }
}
