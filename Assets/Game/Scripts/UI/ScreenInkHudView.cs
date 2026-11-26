using Game.Reef;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Minimal HUD view reflecting normalized screen ink level [0..1] using unscaled time smoothing.
    /// </summary>
    public sealed class ScreenInkHudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScreenInkController _screenInk;
        [SerializeField] private GameObject _inkRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _inkOverlayImage;

        [Header("Visuals")]
        [SerializeField] private Color _inkColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
        [SerializeField, Min(0f)] private float _alphaSmoothSpeed = 12f;

        private float _targetLevel;
        private float _currentLevel;

        private void OnEnable()
        {
            if (_screenInk != null)
            {
                _screenInk.InkChanged += HandleInkChanged;
                _screenInk.InkCleaned += HandleInkCleaned;
            }

            float initialLevel = _screenInk != null ? _screenInk.InkLevel : 0f;
            _targetLevel = initialLevel;
            _currentLevel = initialLevel;
            ApplyVisualLevel(_currentLevel);
        }

        private void OnDisable()
        {
            if (_screenInk != null)
            {
                _screenInk.InkChanged -= HandleInkChanged;
                _screenInk.InkCleaned -= HandleInkCleaned;
            }
        }

        private void Update()
        {
            if (Mathf.Approximately(_currentLevel, _targetLevel))
            {
                return;
            }

            if (_alphaSmoothSpeed <= 0f)
            {
                _currentLevel = _targetLevel;
            }
            else
            {
                _currentLevel = Mathf.MoveTowards(_currentLevel, _targetLevel, _alphaSmoothSpeed * Time.unscaledDeltaTime);
            }

            ApplyVisualLevel(_currentLevel);
        }

        private void HandleInkChanged(float normalizedLevel)
        {
            _targetLevel = Mathf.Clamp01(normalizedLevel);

            if (_targetLevel > 0f && _inkRoot != null && !_inkRoot.activeSelf)
            {
                _inkRoot.SetActive(true);
            }

            if (_alphaSmoothSpeed <= 0f)
            {
                _currentLevel = _targetLevel;
                ApplyVisualLevel(_currentLevel);
            }
        }

        private void HandleInkCleaned()
        {
            HandleInkChanged(0f);
        }

        private void ApplyVisualLevel(float normalizedLevel)
        {
            if (_inkRoot != null)
            {
                bool shouldBeActive = normalizedLevel > 0f || _targetLevel > 0f;
                if (_inkRoot.activeSelf != shouldBeActive)
                {
                    _inkRoot.SetActive(shouldBeActive);
                }
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = normalizedLevel;
                if (_inkOverlayImage != null)
                {
                    _inkOverlayImage.color = _inkColor;
                }

                return;
            }

            if (_inkOverlayImage != null)
            {
                Color color = _inkColor;
                color.a = _inkColor.a * normalizedLevel;
                _inkOverlayImage.color = color;
            }
        }
    }
}
