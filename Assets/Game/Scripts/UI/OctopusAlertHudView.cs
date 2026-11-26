using Game.Reef;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Displays the approaching-enemy HUD warning when the Level 4 octopus appears.
    /// </summary>
    public sealed class OctopusAlertHudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OctopusEnemy _octopusEnemy;
        [SerializeField] private GameObject _alertRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _alertText;
        [SerializeField] private Image _alertIconImage;

        [Header("Formatting & Visuals")]
        [SerializeField] private string _warningMessage = "Un enemigo se aproxima";
        [SerializeField] private Color _warningColor = new Color(1f, 0.45f, 0.15f, 1f);
        [SerializeField, Min(0f)] private float _displayDuration = 3.5f;
        [SerializeField, Min(0f)] private float _fadeSpeed = 10f;

        private float _targetAlpha;
        private float _currentAlpha;
        private float _hideTimer;
        private bool _isShowing;

        private void OnEnable()
        {
            if (_octopusEnemy != null)
            {
                _octopusEnemy.OctopusAppeared += HandleOctopusAppeared;
                _octopusEnemy.OctopusRetreated += HandleOctopusRetreated;
            }

            ApplyAlertVisuals();
            if (_octopusEnemy != null && _octopusEnemy.IsPresent)
            {
                ShowAlert(snapFade: true);
            }
            else
            {
                HideAlert(snapFade: true);
            }
        }

        private void OnDisable()
        {
            if (_octopusEnemy != null)
            {
                _octopusEnemy.OctopusAppeared -= HandleOctopusAppeared;
                _octopusEnemy.OctopusRetreated -= HandleOctopusRetreated;
            }
        }

        private void Update()
        {
            if (_isShowing && _displayDuration > 0f)
            {
                _hideTimer -= Time.unscaledDeltaTime;
                if (_hideTimer <= 0f)
                {
                    HideAlert(snapFade: false);
                }
            }

            if (_canvasGroup == null || _fadeSpeed <= 0f || Mathf.Approximately(_currentAlpha, _targetAlpha))
            {
                return;
            }

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, _fadeSpeed * Time.unscaledDeltaTime);
            _canvasGroup.alpha = _currentAlpha;

            if (_alertRoot != null && _currentAlpha <= 0f && _targetAlpha <= 0f)
            {
                _alertRoot.SetActive(false);
            }
        }

        private void HandleOctopusAppeared()
        {
            ApplyAlertVisuals();
            ShowAlert(snapFade: false);
        }

        private void HandleOctopusRetreated(OctopusRetreatReason _)
        {
            HideAlert(snapFade: false);
        }

        private void ApplyAlertVisuals()
        {
            if (_alertText != null)
            {
                _alertText.text = _warningMessage;
                _alertText.color = _warningColor;
            }

            if (_alertIconImage != null)
            {
                _alertIconImage.color = _warningColor;
            }
        }

        private void ShowAlert(bool snapFade)
        {
            _isShowing = true;
            _hideTimer = _displayDuration;
            _targetAlpha = 1f;

            if (_alertRoot != null)
            {
                _alertRoot.SetActive(true);
            }

            if (_canvasGroup != null && (_fadeSpeed <= 0f || snapFade))
            {
                _currentAlpha = 1f;
                _canvasGroup.alpha = 1f;
            }
        }

        private void HideAlert(bool snapFade)
        {
            _isShowing = false;
            _hideTimer = 0f;
            _targetAlpha = 0f;

            if (_canvasGroup == null || _fadeSpeed <= 0f || snapFade)
            {
                _currentAlpha = 0f;
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                }

                if (_alertRoot != null)
                {
                    _alertRoot.SetActive(false);
                }
            }
        }
    }
}
