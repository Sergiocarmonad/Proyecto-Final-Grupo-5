using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// TEMPORARY greybox debug helper that tints a coral Renderer from CoralZone events via MaterialPropertyBlock.
    /// </summary>
    public sealed class CoralZoneDebugTint : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("References")]
        [SerializeField] private CoralZone _coralZone;
        [SerializeField] private Renderer _targetRenderer;

        [Header("Temporary Greybox Colors")]
        [SerializeField] private Color _cleanColor = new Color(0.2f, 0.8f, 0.3f, 1f);
        [SerializeField] private Color _dirtyingColor = new Color(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color _dirtyColor = new Color(0.28f, 0.16f, 0.08f, 1f);

        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            if (_coralZone == null)
            {
                _coralZone = GetComponent<CoralZone>();
            }

            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponentInChildren<Renderer>();
            }

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (_coralZone == null)
            {
                return;
            }

            _coralZone.CoralDirtyingStarted += HandleZoneStateEvent;
            _coralZone.CleaningProgressed += HandleCleaningProgressed;
            _coralZone.Cleaned += HandleZoneStateEvent;
            ApplyCurrentTint();
        }

        private void OnDisable()
        {
            if (_coralZone == null)
            {
                return;
            }

            _coralZone.CoralDirtyingStarted -= HandleZoneStateEvent;
            _coralZone.CleaningProgressed -= HandleCleaningProgressed;
            _coralZone.Cleaned -= HandleZoneStateEvent;
        }

        private void HandleZoneStateEvent(CoralZone _)
        {
            ApplyCurrentTint();
        }

        private void HandleCleaningProgressed(float _)
        {
            ApplyCurrentTint();
        }

        private void ApplyCurrentTint()
        {
            if (_coralZone == null || _targetRenderer == null || _propertyBlock == null)
            {
                return;
            }

            Color tint = EvaluateTintColor();
            _targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, tint);
            _propertyBlock.SetColor(ColorId, tint);
            _targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Color EvaluateTintColor()
        {
            if (_coralZone.IsClean)
            {
                return _cleanColor;
            }

            return Color.Lerp(_dirtyingColor, _dirtyColor, _coralZone.Dirtiness);
        }
    }
}
