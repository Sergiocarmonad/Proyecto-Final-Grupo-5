using System.Collections.Generic;
using Game.Radar;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Displays active radar blips inside a fixed uGUI radar container from RadarController snapshots.
    /// </summary>
    public sealed class RadarHudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RadarController _radarController;
        [SerializeField] private RectTransform _radarArea;
        [SerializeField] private Image _blipPrefab;

        [Header("Pool & Layout")]
        [SerializeField, Min(1)] private int _blipPoolSize = 32;
        [SerializeField, Min(0f)] private float _radarRadiusOverride;
        [SerializeField, Min(0f)] private float _edgePadding = 6f;

        [Header("Signal Colors")]
        [SerializeField] private Color _greenColor = new Color(0.25f, 0.9f, 0.4f, 1f);
        [SerializeField] private Color _orangeColor = new Color(1f, 0.58f, 0.15f, 1f);
        [SerializeField] private Color _redColor = new Color(0.95f, 0.25f, 0.25f, 1f);

        private readonly List<Image> _blipPool = new List<Image>();
        private bool _poolInitialized;

        private void Awake()
        {
            EnsurePoolInitialized();
        }

        private void OnEnable()
        {
            EnsurePoolInitialized();

            if (_radarController == null)
            {
                HideAllBlips();
                return;
            }

            _radarController.RadarUpdated += HandleRadarUpdated;
            ApplyBlips(_radarController.CurrentBlips);
        }

        private void OnDisable()
        {
            if (_radarController != null)
            {
                _radarController.RadarUpdated -= HandleRadarUpdated;
            }
        }

        private void EnsurePoolInitialized()
        {
            if (_poolInitialized || _blipPrefab == null || _radarArea == null)
            {
                return;
            }

            _poolInitialized = true;

            if (_blipPrefab.gameObject.scene.IsValid())
            {
                _blipPrefab.gameObject.SetActive(false);
            }

            for (int i = 0; i < _blipPoolSize; i++)
            {
                Image instance = Instantiate(_blipPrefab, _radarArea);
                instance.gameObject.SetActive(false);
                _blipPool.Add(instance);
            }
        }

        private void HandleRadarUpdated(IReadOnlyList<RadarBlipData> blips)
        {
            ApplyBlips(blips);
        }

        private void ApplyBlips(IReadOnlyList<RadarBlipData> blips)
        {
            if (!_poolInitialized || _radarArea == null)
            {
                return;
            }

            float maxRadius = ResolveRadarRadius();
            int usedCount = 0;

            if (blips != null)
            {
                for (int i = 0; i < blips.Count && usedCount < _blipPool.Count; i++)
                {
                    RadarBlipData blip = blips[i];
                    if (!blip.IsActive)
                    {
                        continue;
                    }

                    Image blipImage = _blipPool[usedCount++];
                    blipImage.gameObject.SetActive(true);
                    blipImage.color = ResolveSignalColor(blip.Color);

                    float angleRad = blip.RelativeAngleDegrees * Mathf.Deg2Rad;
                    float distance = Mathf.Clamp01(blip.NormalizedDistance) * maxRadius;
                    blipImage.rectTransform.anchoredPosition = new Vector2(
                        Mathf.Sin(angleRad) * distance,
                        Mathf.Cos(angleRad) * distance);
                }
            }

            for (int i = usedCount; i < _blipPool.Count; i++)
            {
                if (_blipPool[i].gameObject.activeSelf)
                {
                    _blipPool[i].gameObject.SetActive(false);
                }
            }
        }

        private void HideAllBlips()
        {
            for (int i = 0; i < _blipPool.Count; i++)
            {
                if (_blipPool[i].gameObject.activeSelf)
                {
                    _blipPool[i].gameObject.SetActive(false);
                }
            }
        }

        private float ResolveRadarRadius()
        {
            if (_radarRadiusOverride > 0f)
            {
                return _radarRadiusOverride;
            }

            Rect rect = _radarArea.rect;
            return Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f - _edgePadding);
        }

        private Color ResolveSignalColor(RadarSignalColor signalColor)
        {
            switch (signalColor)
            {
                case RadarSignalColor.Green:
                    return _greenColor;
                case RadarSignalColor.Orange:
                    return _orangeColor;
                case RadarSignalColor.Red:
                    return _redColor;
                default:
                    return _greenColor;
            }
        }
    }
}
