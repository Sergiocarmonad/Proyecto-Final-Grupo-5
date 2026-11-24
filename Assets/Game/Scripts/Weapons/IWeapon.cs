using System;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Common contract for player weapons supporting activation, aiming use, ammo, and reloading.
    /// </summary>
    public interface IWeapon
    {
        WeaponConfig Config { get; }
        bool UsesAmmo { get; }
        int CurrentAmmo { get; }
        int MaxAmmo { get; }
        bool IsReloading { get; }
        bool IsOnCooldown { get; }
        bool IsActive { get; }

        /// <summary>
        /// Raised when current or max ammo changes, passing (currentAmmo, maxAmmo).
        /// </summary>
        event Action<int, int> AmmoChanged;

        /// <summary>
        /// Raised when a reload begins, passing the reload duration in seconds.
        /// </summary>
        event Action<float> ReloadStarted;

        /// <summary>
        /// Raised when a reload completes and ammo is replenished.
        /// </summary>
        event Action ReloadFinished;

        /// <summary>
        /// Raised when a weapon use is attempted while out of ammo.
        /// </summary>
        event Action EmptyTriggered;

        /// <summary>
        /// Raised when the weapon successfully executes a use with the aiming ray.
        /// </summary>
        event Action<Ray> UseExecuted;

        void SetActive(bool active);
        bool TryUse(Ray aimRay);
        bool TryReload();
    }
}
