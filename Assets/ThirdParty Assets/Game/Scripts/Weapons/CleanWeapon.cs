using Game.Reef;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Cleaning weapon that sprays along the aiming ray and consumes tank ammo only when cleaning a dirty coral zone.
    /// </summary>
    public sealed class CleanWeapon : WeaponBase
    {
        [SerializeField, Min(0.001f)] private float _cleanAmountPerUse = 0.05f;

        private bool _didCleanOnLastUse;

        protected override bool ShouldConsumeAmmoAfterUse => _didCleanOnLastUse;

        protected override void ExecuteUse(Ray aimRay)
        {
            _didCleanOnLastUse = false;

            if (!Physics.Raycast(aimRay, out RaycastHit hit, Config.Range, Config.HitMask, QueryTriggerInteraction.Collide))
            {
                return;
            }

            CoralZone coralZone = hit.collider.GetComponentInParent<CoralZone>();
            if (coralZone == null || coralZone.IsClean)
            {
                return;
            }

            _didCleanOnLastUse = coralZone.ApplyCleaning(_cleanAmountPerUse);
        }
    }
}
