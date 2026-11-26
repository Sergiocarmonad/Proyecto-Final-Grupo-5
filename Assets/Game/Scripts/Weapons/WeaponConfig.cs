using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Configurable parameters for weapon ammo, reload, cooldown, and aiming behavior.
    /// </summary>
    [CreateAssetMenu(menuName = "Coral Rescue/Weapon Config", fileName = "WeaponConfig")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("Wheel Metadata")]
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        [Header("Ammo & Reload")]
        [SerializeField] private bool _usesAmmo = true;
        [SerializeField, Min(0)] private int _maxAmmo = 10;
        [SerializeField, Min(0f)] private float _reloadDuration = 1.5f;

        [Header("Firing & Aiming")]
        [SerializeField, Min(0f)] private float _cooldownDuration = 0.25f;
        [SerializeField] private bool _continuousUse;
        [SerializeField, Min(0.1f)] private float _range = 30f;
        [SerializeField, Min(0f)] private float _aimRadius = 0.2f;
        [SerializeField] private LayerMask _hitMask = ~0;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public Sprite Icon => _icon;
        public bool UsesAmmo => _usesAmmo;
        public int MaxAmmo => _usesAmmo ? Mathf.Max(0, _maxAmmo) : 0;
        public float ReloadDuration => Mathf.Max(0f, _reloadDuration);
        public float CooldownDuration => Mathf.Max(0f, _cooldownDuration);
        public bool ContinuousUse => _continuousUse;
        public float Range => Mathf.Max(0.1f, _range);
        public float AimRadius => Mathf.Max(0f, _aimRadius);
        public LayerMask HitMask => _hitMask;
    }
}
