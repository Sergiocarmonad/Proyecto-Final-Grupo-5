using Game.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Displays the remaining level survival time from LevelFlowController events.
    /// </summary>
    public sealed class LevelTimerHudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelFlowController _levelFlowController;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Slider _timerSlider;

        [Header("Formatting & Visuals")]
        [SerializeField] private string _timeFormat = "{0:00}:{1:00}";
        [SerializeField, Min(0f)] private float _lowTimeThresholdSeconds = 15f;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _lowTimeColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField, Min(0f)] private float _fillSmoothSpeed = 12f;

        private float _targetNormalized;
        private float _displayedNormalized;

        private void OnEnable()
        {
            if (_levelFlowController == null)
            {
                return;
            }

            _levelFlowController.LevelStarted += HandleLevelStarted;
            _levelFlowController.TimeRemainingChanged += HandleTimeRemainingChanged;
            _levelFlowController.LevelWon += HandleLevelEnded;
            _levelFlowController.LevelLost += HandleLevelEnded;

            ApplyTime(_levelFlowController.RemainingTime, _levelFlowController.Duration, snapVisuals: true);
        }

        private void OnDisable()
        {
            if (_levelFlowController == null)
            {
                return;
            }

            _levelFlowController.LevelStarted -= HandleLevelStarted;
            _levelFlowController.TimeRemainingChanged -= HandleTimeRemainingChanged;
            _levelFlowController.LevelWon -= HandleLevelEnded;
            _levelFlowController.LevelLost -= HandleLevelEnded;
        }

        private void Update()
        {
            if (_fillSmoothSpeed <= 0f || Mathf.Approximately(_displayedNormalized, _targetNormalized))
            {
                return;
            }

            _displayedNormalized = Mathf.MoveTowards(
                _displayedNormalized,
                _targetNormalized,
                _fillSmoothSpeed * Time.unscaledDeltaTime);

            ApplyNormalizedValue(_displayedNormalized);
        }

        private void HandleLevelStarted(LevelConfig _)
        {
            ApplyTime(_levelFlowController.RemainingTime, _levelFlowController.Duration, snapVisuals: true);
        }

        private void HandleTimeRemainingChanged(float remainingSeconds, float durationSeconds)
        {
            ApplyTime(remainingSeconds, durationSeconds, snapVisuals: false);
        }

        private void HandleLevelEnded(LevelConfig _)
        {
            ApplyTime(_levelFlowController.RemainingTime, _levelFlowController.Duration, snapVisuals: true);
        }

        private void ApplyTime(float remainingSeconds, float durationSeconds, bool snapVisuals)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            bool isLowTime = durationSeconds > 0f && remainingSeconds <= _lowTimeThresholdSeconds;
            Color activeColor = isLowTime ? _lowTimeColor : _normalColor;

            if (_timerText != null)
            {
                _timerText.text = string.Format(_timeFormat, minutes, seconds);
                _timerText.color = activeColor;
            }

            if (_fillImage != null)
            {
                _fillImage.color = activeColor;
            }

            _targetNormalized = durationSeconds > 0f ? Mathf.Clamp01(remainingSeconds / durationSeconds) : 0f;
            if (snapVisuals || _fillSmoothSpeed <= 0f)
            {
                _displayedNormalized = _targetNormalized;
                ApplyNormalizedValue(_displayedNormalized);
            }
        }

        private void ApplyNormalizedValue(float normalized)
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = normalized;
            }

            if (_timerSlider != null)
            {
                _timerSlider.value = normalized;
            }
        }
    }
}
