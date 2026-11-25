using System.Collections.Generic;
using Game.Reef;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Displays coral contamination alerts from CoralAlertNotifier, supporting multiple active alerts.
    /// </summary>
    public sealed class CoralAlertHudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CoralAlertNotifier _alertNotifier;
        [SerializeField] private GameObject _alertRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _alertText;
        [SerializeField] private Image _alertIconImage;

        [Header("Formatting & Visuals")]
        [SerializeField] private string _singleAlertFormat = "Coral getting dirty: {0}";
        [SerializeField] private string _multipleAlertsFormat = "{0} corals dirty! Latest: {1}";
        [SerializeField] private string _fallbackCountFormat = "{0} coral zone(s) getting dirty!";
        [SerializeField] private string _unnamedZoneLabel = "Unknown Zone";
        [SerializeField] private Color _alertColor = new Color(1f, 0.55f, 0.15f, 1f);
        [SerializeField, Min(0f)] private float _fadeSpeed = 10f;

        private readonly List<CoralZone> _recentAlertZones = new List<CoralZone>();
        private float _targetAlpha;
        private float _currentAlpha;

        private void OnEnable()
        {
            if (_alertNotifier == null)
            {
                return;
            }

            _alertNotifier.AlertRaised += HandleAlertRaised;
            _alertNotifier.AlertCleared += HandleAlertCleared;

            _recentAlertZones.Clear();
            RefreshDisplay(snapFade: true);
        }

        private void OnDisable()
        {
            if (_alertNotifier != null)
            {
                _alertNotifier.AlertRaised -= HandleAlertRaised;
                _alertNotifier.AlertCleared -= HandleAlertCleared;
            }

            _recentAlertZones.Clear();
        }

        private void Update()
        {
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

        private void HandleAlertRaised(CoralZone zone)
        {
            if (zone != null)
            {
                _recentAlertZones.Remove(zone);
                _recentAlertZones.Add(zone);
            }

            RefreshDisplay(snapFade: false);
        }

        private void HandleAlertCleared(CoralZone zone)
        {
            if (zone != null)
            {
                _recentAlertZones.Remove(zone);
            }

            RefreshDisplay(snapFade: false);
        }

        private void RefreshDisplay(bool snapFade)
        {
            int activeCount = _alertNotifier != null ? _alertNotifier.ActiveAlertCount : 0;
            bool hasAlerts = activeCount > 0;

            if (!hasAlerts)
            {
                _recentAlertZones.Clear();
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

                return;
            }

            if (_alertRoot != null)
            {
                _alertRoot.SetActive(true);
            }

            _targetAlpha = 1f;
            if (_canvasGroup != null && (_fadeSpeed <= 0f || snapFade))
            {
                _currentAlpha = 1f;
                _canvasGroup.alpha = 1f;
            }

            CoralZone latestZone = GetLatestValidZone();

            if (_alertText != null)
            {
                _alertText.color = _alertColor;

                if (latestZone == null)
                {
                    _alertText.text = string.Format(_fallbackCountFormat, activeCount);
                }
                else
                {
                    string zoneLabel = string.IsNullOrWhiteSpace(latestZone.ZoneId) ? _unnamedZoneLabel : latestZone.ZoneId;
                    _alertText.text = activeCount == 1
                        ? string.Format(_singleAlertFormat, zoneLabel)
                        : string.Format(_multipleAlertsFormat, activeCount, zoneLabel);
                }
            }

            if (_alertIconImage != null)
            {
                _alertIconImage.color = _alertColor;
            }
        }

        private CoralZone GetLatestValidZone()
        {
            for (int i = _recentAlertZones.Count - 1; i >= 0; i--)
            {
                if (_recentAlertZones[i] != null)
                {
                    return _recentAlertZones[i];
                }

                _recentAlertZones.RemoveAt(i);
            }

            return null;
        }
    }
}
