using Game.Reef;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Displays shared reef and player health from ReefHealth events.
    /// </summary>
    public sealed class ReefHealthHudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ReefHealth _reefHealth;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Slider _healthSlider;

        [Header("Formatting & Visuals")]
        [SerializeField] private string _textFormat = "{0:0}/{1:0}";
        [SerializeField, Range(0f, 1f)] private float _lowHealthThreshold = 0.3f;
        [SerializeField] private Color _healthyColor = new Color(0.25f, 0.85f, 0.45f, 1f);
        [SerializeField] private Color _lowHealthColor = new Color(0.95f, 0.25f, 0.25f, 1f);
        [SerializeField, Min(0f)] private float _fillSmoothSpeed = 12f;

        private float _targetNormalized;
        private float _displayedNormalized;

        private void OnEnable()
        {
            if (_reefHealth == null)
            {
                return;
            }

            _reefHealth.HealthChanged += HandleHealthChanged;
            ApplyHealth(_reefHealth.CurrentHealth, _reefHealth.MaxHealth, snapVisuals: true);
        }

        private void OnDisable()
        {
            if (_reefHealth == null)
            {
                return;
            }

            _reefHealth.HealthChanged -= HandleHealthChanged;
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

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            ApplyHealth(currentHealth, maxHealth, snapVisuals: false);
        }

        private void ApplyHealth(float currentHealth, float maxHealth, bool snapVisuals)
        {
            float normalized = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            Color activeColor = normalized <= _lowHealthThreshold ? _lowHealthColor : _healthyColor;

            if (_healthText != null)
            {
                _healthText.text = string.Format(_textFormat, Mathf.Max(0f, currentHealth), Mathf.Max(0f, maxHealth));
            }

            if (_fillImage != null)
            {
                _fillImage.color = activeColor;
            }

            _targetNormalized = normalized;
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

            if (_healthSlider != null)
            {
                _healthSlider.value = normalized;
            }
        }
    }
}
