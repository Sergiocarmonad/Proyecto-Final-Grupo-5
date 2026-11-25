using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// Rotates a static first-person camera horizontally (360 degrees) and vertically (clamped)
    /// when the free cursor enters configurable screen edge zones.
    /// </summary>
    public sealed class EdgeLookCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _cameraPivot;

        [Header("Edge Zones (Screen Fraction)")]
        [SerializeField, Range(0.01f, 0.45f)] private float _horizontalEdgeZone = 0.12f;
        [SerializeField, Range(0.01f, 0.45f)] private float _verticalEdgeZone = 0.10f;

        [Header("Rotation Speed (Degrees/Second)")]
        [SerializeField, Min(0f)] private float _horizontalSpeed = 90f;
        [SerializeField, Min(0f)] private float _verticalSpeed = 35f;

        [Header("Vertical Limits (Degrees)")]
        [SerializeField, Range(-89f, 0f)] private float _minPitch = -12f;
        [SerializeField, Range(0f, 89f)] private float _maxPitch = 12f;

        [Header("Behavior")]
        [SerializeField] private bool _useUnscaledTime = true;
        [SerializeField] private bool _ignoreOutsideWindow = true;
        [SerializeField] private bool _requireNeutralZoneOnStart = true;

        private float _yaw;
        private float _pitch;
        private bool _rotationEnabled = true;
        private bool _hasEnteredNeutralZone;
        private Vector2 _lastEdgeInput;

        /// <summary>
        /// Emitted when normalized edge rotation input (-1..1 per axis) changes.
        /// </summary>
        public event Action<Vector2> EdgeInputChanged;

        /// <summary>
        /// Emitted whenever the camera orientation updates with current (yaw, pitch) in degrees.
        /// </summary>
        public event Action<float, float> OrientationChanged;

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        private void Awake()
        {
            if (_cameraPivot == null)
            {
                _cameraPivot = transform;
            }

            Vector3 euler = _cameraPivot.localEulerAngles;
            _yaw = euler.y;
            _pitch = NormalizeSignedAngle(euler.x);
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
            _cameraPivot.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void OnEnable()
        {
            _hasEnteredNeutralZone = !_requireNeutralZoneOnStart;
            ApplyCursorState();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _hasEnteredNeutralZone = !_requireNeutralZoneOnStart;
                UpdateEdgeInputEvent(Vector2.zero);
                return;
            }

            ApplyCursorState();
        }

        private void OnValidate()
        {
            if (_minPitch > _maxPitch)
            {
                _maxPitch = _minPitch;
            }
        }

        private void Update()
        {
            if (!_rotationEnabled || !Application.isFocused)
            {
                UpdateEdgeInputEvent(Vector2.zero);
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || Screen.width <= 0 || Screen.height <= 0)
            {
                UpdateEdgeInputEvent(Vector2.zero);
                return;
            }

            Vector2 mousePos = mouse.position.ReadValue();
            float normalizedX = mousePos.x / Screen.width;
            float normalizedY = mousePos.y / Screen.height;

            if (_ignoreOutsideWindow && (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f))
            {
                UpdateEdgeInputEvent(Vector2.zero);
                return;
            }

            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);

            Vector2 edgeInput = new Vector2(
                CalculateEdgeAxis(normalizedX, _horizontalEdgeZone),
                CalculateEdgeAxis(normalizedY, _verticalEdgeZone));

            if (!_hasEnteredNeutralZone)
            {
                if (edgeInput == Vector2.zero)
                {
                    _hasEnteredNeutralZone = true;
                }

                UpdateEdgeInputEvent(Vector2.zero);
                return;
            }

            UpdateEdgeInputEvent(edgeInput);

            if (edgeInput == Vector2.zero)
            {
                return;
            }

            float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _yaw = Mathf.Repeat(_yaw + edgeInput.x * _horizontalSpeed * deltaTime, 360f);
            _pitch = Mathf.Clamp(_pitch - edgeInput.y * _verticalSpeed * deltaTime, _minPitch, _maxPitch);

            _cameraPivot.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            OrientationChanged?.Invoke(_yaw, _pitch);
        }

        /// <summary>
        /// Enables or disables edge-based camera rotation without altering cursor visibility.
        /// </summary>
        public void SetRotationEnabled(bool isEnabled)
        {
            _rotationEnabled = isEnabled;
            if (!isEnabled)
            {
                UpdateEdgeInputEvent(Vector2.zero);
            }
        }

        private static void ApplyCursorState()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        private static float CalculateEdgeAxis(float normalizedCoord, float edgeZone)
        {
            if (normalizedCoord < edgeZone)
            {
                return -Mathf.Clamp01((edgeZone - normalizedCoord) / edgeZone);
            }

            float upperThreshold = 1f - edgeZone;
            if (normalizedCoord > upperThreshold)
            {
                return Mathf.Clamp01((normalizedCoord - upperThreshold) / edgeZone);
            }

            return 0f;
        }

        private static float NormalizeSignedAngle(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }

        private void UpdateEdgeInputEvent(Vector2 edgeInput)
        {
            if (edgeInput == _lastEdgeInput)
            {
                return;
            }

            _lastEdgeInput = edgeInput;
            EdgeInputChanged?.Invoke(edgeInput);
        }
    }
}
