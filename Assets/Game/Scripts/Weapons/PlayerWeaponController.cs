using System;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Weapons
{
    /// <summary>
    /// Reads weapon fire and reload input and forwards cursor aiming rays to the active IWeapon.
    /// </summary>
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private CursorAimRayProvider _aimRayProvider;
        [SerializeField] private WeaponBase _initialWeapon;

        private IWeapon _activeWeapon;
        private bool _isInputBlocked;

        /// <summary>
        /// Raised when the active equipped weapon changes.
        /// </summary>
        public event Action<IWeapon> WeaponChanged;

        public IWeapon ActiveWeapon => _activeWeapon;

        private void Awake()
        {
            if (_initialWeapon != null)
            {
                EquipWeapon(_initialWeapon);
            }
        }

        private void Update()
        {
            if (_isInputBlocked || Time.timeScale <= 0f || !Application.isFocused || _activeWeapon == null)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            bool reloadPressed = (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                || (mouse != null && mouse.rightButton.wasPressedThisFrame);
            if (reloadPressed)
            {
                _activeWeapon.TryReload();
            }

            if (mouse == null || _aimRayProvider == null)
            {
                return;
            }

            bool continuousUse = _activeWeapon.Config != null && _activeWeapon.Config.ContinuousUse;
            bool usePressed = continuousUse ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
            if (!usePressed)
            {
                return;
            }

            if (_aimRayProvider.TryGetAimRay(out Ray aimRay))
            {
                _activeWeapon.TryUse(aimRay);
            }
        }

        /// <summary>
        /// Blocks or unblocks player fire and reload input (e.g. while the weapon wheel is open).
        /// </summary>
        public void SetInputBlocked(bool blocked)
        {
            _isInputBlocked = blocked;
        }

        /// <summary>
        /// Switches the active weapon controlled by player input.
        /// </summary>
        public void EquipWeapon(IWeapon weapon)
        {
            if (_activeWeapon == weapon)
            {
                return;
            }

            _activeWeapon?.SetActive(false);
            _activeWeapon = weapon;
            _activeWeapon?.SetActive(true);
            WeaponChanged?.Invoke(_activeWeapon);
        }
    }
}
