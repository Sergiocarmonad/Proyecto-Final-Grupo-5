using Game.Reef;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Cleaning weapon that sprays along the aiming ray and consumes tank ammo only when cleaning a dirty coral zone or screen ink.
    /// </summary>
    public sealed class CleanWeapon : WeaponBase
    {
        [SerializeField, Min(0.001f)] private float _cleanAmountPerUse = 0.05f;
        [SerializeField] private ScreenInkController _screenInk;
        [SerializeField, Min(0.001f)] private float _inkCleanAmountPerUse = 0.25f;

        private bool _didCleanOnLastUse;

        protected override bool ShouldConsumeAmmoAfterUse => _didCleanOnLastUse;

        protected override void ExecuteUse(Ray aimRay)
        {
            _didCleanOnLastUse = false;

            if (Physics.Raycast(aimRay, out RaycastHit hit, Config.Range, Config.HitMask, QueryTriggerInteraction.Collide))
            {
                CoralZone coralZone = hit.collider.GetComponentInParent<CoralZone>();
                if (coralZone != null && !coralZone.IsClean)
                {
                    _didCleanOnLastUse = coralZone.ApplyCleaning(_cleanAmountPerUse);
                    return;
                }
            }

            if (_screenInk != null && _screenInk.HasInk)
            {
                _didCleanOnLastUse = _screenInk.CleanInk(_inkCleanAmountPerUse);
            }
        }
    }
}
