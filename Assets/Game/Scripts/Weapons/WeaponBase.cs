using System;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Base weapon simulation handling ammo consumption, reload timing, cooldowns, and weapon events.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour, IWeapon
    {
        [SerializeField] private WeaponConfig _config;
        [SerializeField] private bool _startActive = true;

        private int _currentAmmo;
        private bool _isReloading;
        private float _reloadTimer;
        private float _cooldownTimer;
        private bool _isActive;

        public event Action<int, int> AmmoChanged;
        public event Action<float> ReloadStarted;
        public event Action ReloadFinished;
        public event Action EmptyTriggered;
        public event Action<Ray> UseExecuted;

        public WeaponConfig Config => _config;
        public bool UsesAmmo => _config != null && _config.UsesAmmo;
        public int CurrentAmmo => _currentAmmo;
        public int MaxAmmo => _config != null ? _config.MaxAmmo : 0;
        public bool IsReloading => _isReloading;
        public bool IsOnCooldown => _cooldownTimer > 0f;
        public bool IsActive => _isActive;

        protected virtual void Awake()
        {
            _isActive = _startActive;
            InitializeAmmo();
        }

        protected virtual void Update()
        {
            float deltaTime = Time.deltaTime;
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - deltaTime);
            }

            if (!_isReloading)
            {
                return;
            }

            _reloadTimer -= deltaTime;
            if (_reloadTimer <= 0f)
            {
                CompleteReload();
            }
        }

        /// <summary>
        /// Enables or disables this weapon. Deactivating cancels any active reload.
        /// </summary>
        public void SetActive(bool active)
        {
            if (_isActive == active)
            {
                return;
            }

            _isActive = active;
            if (!_isActive)
            {
                _isReloading = false;
                _reloadTimer = 0f;
                return;
            }

            AmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
        }

        /// <summary>
        /// Attempts to fire or activate the weapon along the provided aiming ray.
        /// </summary>
        public bool TryUse(Ray aimRay)
        {
            if (!_isActive || _config == null || _isReloading || IsOnCooldown)
            {
                return false;
            }

            if (UsesAmmo && _currentAmmo <= 0)
            {
                AkUnitySoundEngine.PostEvent("Empty_Ammo", gameObject);
                EmptyTriggered?.Invoke();
                return false;
            }

            ExecuteUse(aimRay);
            UseExecuted?.Invoke(aimRay);

            if (UsesAmmo && ShouldConsumeAmmoAfterUse)
            {
                _currentAmmo = Mathf.Max(0, _currentAmmo - 1);
                AmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
            }

            _cooldownTimer = _config.CooldownDuration;
            return true;
        }

        /// <summary>
        /// Starts reloading when the weapon uses ammo and is not already full or reloading.
        /// </summary>
        public bool TryReload()
        {
            if (!UsesAmmo || !_isActive || _isReloading || _currentAmmo >= MaxAmmo)
            {
                return false;
            }

            float reloadDuration = _config.ReloadDuration;
            ReloadStarted?.Invoke(reloadDuration);

            if (reloadDuration <= 0f)
            {
                CompleteReload();
                return true;
            }

            _isReloading = true;
            _reloadTimer = reloadDuration;
            return true;
        }

        /// <summary>
        /// Returns whether the last executed use should consume one unit of ammo.
        /// </summary>
        protected virtual bool ShouldConsumeAmmoAfterUse => true;

        /// <summary>
        /// Performs weapon-specific targeting or simulation when TryUse succeeds.
        /// </summary>
        protected abstract void ExecuteUse(Ray aimRay);

        private void InitializeAmmo()
        {
            _currentAmmo = UsesAmmo ? MaxAmmo : 0;
            AmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
        }

        private void CompleteReload()
        {
            _isReloading = false;
            _reloadTimer = 0f;
            _currentAmmo = MaxAmmo;
            ReloadFinished?.Invoke();
            AmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
        }
    }
}
