using System;
using System.Collections.Generic;
using Game.Flow;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Game.Weapons
{
    /// <summary>
    /// Handles GTA V-style hold-to-open weapon wheel logic, slow-motion scaling, and radial cursor selection.
    /// </summary>
    public sealed class WeaponWheelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWeaponController _weaponController;
        [SerializeField] private EdgeLookCameraController _edgeLookCamera;
        [SerializeField] private LevelFlowController _levelFlowController;
        [SerializeField] private List<WeaponBase> _weapons = new List<WeaponBase>();

        [Header("Input & Time")]
        [SerializeField] private Key _wheelKey = Key.Tab;
        [SerializeField, Range(0.05f, 1f)] private float _slowMotionTimeScale = 0.2f;

        [Header("Radial Selection")]
        [SerializeField, Range(0.01f, 0.4f)] private float _deadzoneRadiusFraction = 0.06f;
        [SerializeField] private float _startAngleDegrees = 90f;
        [SerializeField] private bool _clockwise = true;

        private bool _isOpen;
        private int _hoveredIndex = -1;
        private bool _hasTimeOverride;
        private float _previousTimeScale = 1f;
        private float _previousFixedDeltaTime = 0.02f;

        /// <summary>
        /// Raised when the weapon wheel opens and slow-motion begins.
        /// </summary>
        public event Action WheelOpened;

        /// <summary>
        /// Raised when the weapon wheel closes and normal time resumes.
        /// </summary>
        public event Action WheelClosed;

        /// <summary>
        /// Raised when the hovered wheel slot changes, passing (slotIndex, weapon) or (-1, null) in deadzone.
        /// </summary>
        public event Action<int, IWeapon> HoveredSlotChanged;

        public bool IsOpen => _isOpen;
        public int HoveredIndex => _hoveredIndex;
        public IReadOnlyList<WeaponBase> Weapons => _weapons;
        public float SlowMotionTimeScale => _slowMotionTimeScale;

        private void Start()
        {
            SyncInactiveWeapons();
        }

        private void OnEnable()
        {
            if (_levelFlowController != null)
            {
                _levelFlowController.LevelWon += HandleLevelEnded;
                _levelFlowController.LevelLost += HandleLevelEnded;
            }
        }

        private void OnDisable()
        {
            if (_levelFlowController != null)
            {
                _levelFlowController.LevelWon -= HandleLevelEnded;
                _levelFlowController.LevelLost -= HandleLevelEnded;
            }

            Close(applySelection: false);
            RestoreTimeScale();
        }

        private void OnDestroy()
        {
            Close(applySelection: false);
            RestoreTimeScale();
        }

        private void Update()
        {
            KeyControl keyControl = GetWheelKeyControl();

            if (!_isOpen)
            {
                if (keyControl != null && keyControl.wasPressedThisFrame && CanOpenWheel())
                {
                    Open();
                }

                return;
            }

            UpdateHoveredSlotFromCursor();

            if (keyControl == null || keyControl.wasReleasedThisFrame || !keyControl.isPressed)
            {
                Close(applySelection: true);
            }
        }

        /// <summary>
        /// Allows external UI to override the currently hovered slot index (-1 for none).
        /// </summary>
        public void SelectSlot(int index)
        {
            if (!_isOpen)
            {
                return;
            }

            int normalized = IsValidWeaponIndex(index) ? index : -1;
            SetHoveredIndex(normalized);
        }

        private void Open()
        {
            if (_isOpen)
            {
                return;
            }

            _isOpen = true;
            _hoveredIndex = -1;

            ApplySlowMotion();
            _weaponController?.SetInputBlocked(true);
            _edgeLookCamera?.SetRotationEnabled(false);

            WheelOpened?.Invoke();
            UpdateHoveredSlotFromCursor();
        }

        private void Close(bool applySelection)
        {
            if (!_isOpen)
            {
                return;
            }

            if (applySelection && IsValidWeaponIndex(_hoveredIndex) && _weaponController != null)
            {
                _weaponController.EquipWeapon(_weapons[_hoveredIndex]);
            }

            _isOpen = false;
            SetHoveredIndex(-1);

            RestoreTimeScale();
            _weaponController?.SetInputBlocked(false);
            _edgeLookCamera?.SetRotationEnabled(true);

            WheelClosed?.Invoke();
        }

        private void HandleLevelEnded(LevelConfig _)
        {
            Close(applySelection: false);
        }

        private bool CanOpenWheel()
        {
            return Time.timeScale > 0f && Application.isFocused && _weapons.Count > 0;
        }

        private KeyControl GetWheelKeyControl()
        {
            if (_wheelKey == Key.None)
            {
                return null;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null ? keyboard[_wheelKey] : null;
        }

        private void UpdateHoveredSlotFromCursor()
        {
            int count = _weapons.Count;
            Mouse mouse = Mouse.current;
            int minDimension = Mathf.Min(Screen.width, Screen.height);
            if (mouse == null || count <= 0 || minDimension <= 0)
            {
                SetHoveredIndex(-1);
                return;
            }

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 offset = mouse.position.ReadValue() - screenCenter;
            float deadzonePixels = minDimension * _deadzoneRadiusFraction;

            if (offset.sqrMagnitude < deadzonePixels * deadzonePixels)
            {
                SetHoveredIndex(-1);
                return;
            }

            float cursorAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            float deltaAngle = _clockwise ? (_startAngleDegrees - cursorAngle) : (cursorAngle - _startAngleDegrees);
            float sectorSize = 360f / count;
            float shiftedAngle = Mathf.Repeat(deltaAngle + sectorSize * 0.5f, 360f);
            int slotIndex = Mathf.Clamp(Mathf.FloorToInt(shiftedAngle / sectorSize), 0, count - 1);

            SetHoveredIndex(IsValidWeaponIndex(slotIndex) ? slotIndex : -1);
        }

        private void SetHoveredIndex(int index)
        {
            if (_hoveredIndex == index)
            {
                return;
            }

            _hoveredIndex = index;
            IWeapon hoveredWeapon = IsValidWeaponIndex(_hoveredIndex) ? _weapons[_hoveredIndex] : null;
            HoveredSlotChanged?.Invoke(_hoveredIndex, hoveredWeapon);
        }

        private bool IsValidWeaponIndex(int index)
        {
            return index >= 0 && index < _weapons.Count && _weapons[index] != null;
        }

        private void SyncInactiveWeapons()
        {
            IWeapon activeWeapon = _weaponController != null ? _weaponController.ActiveWeapon : null;
            for (int i = 0; i < _weapons.Count; i++)
            {
                WeaponBase weapon = _weapons[i];
                if (weapon != null && !ReferenceEquals(weapon, activeWeapon))
                {
                    weapon.SetActive(false);
                }
            }
        }

        private void ApplySlowMotion()
        {
            if (!_hasTimeOverride)
            {
                _previousTimeScale = Time.timeScale;
                _previousFixedDeltaTime = Time.fixedDeltaTime;
                _hasTimeOverride = true;
            }

            float targetScale = Mathf.Clamp(_slowMotionTimeScale, 0.05f, 1f);
            float scaleRatio = _previousTimeScale > 0f ? targetScale / _previousTimeScale : targetScale;
            Time.timeScale = targetScale;
            Time.fixedDeltaTime = Mathf.Max(0.0001f, _previousFixedDeltaTime * scaleRatio);
        }

        private void RestoreTimeScale()
        {
            if (!_hasTimeOverride)
            {
                return;
            }

            Time.timeScale = _previousTimeScale;
            Time.fixedDeltaTime = _previousFixedDeltaTime;
            _hasTimeOverride = false;
        }
    }
}
