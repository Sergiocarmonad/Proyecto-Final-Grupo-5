using System;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Hunting weapon that casts along the aiming ray to catch ICatchable targets.
    /// </summary>
    public sealed class HuntingWeapon : WeaponBase
    {
        /// <summary>
        /// Raised when a catchable target is hit and caught, passing (hitPoint, targetObject).
        /// </summary>
        public event Action<Vector3, GameObject> HuntShotHit;

        /// <summary>
        /// Raised when a shot finds no eligible catchable target, passing the aiming ray.
        /// </summary>
        public event Action<Ray> HuntShotMissed;

        protected override void ExecuteUse(Ray aimRay)
        {
            if (!TryCastForTarget(aimRay, out RaycastHit hit))
            {
                HuntShotMissed?.Invoke(aimRay);
                return;
            }

            ICatchable catchable = hit.collider.GetComponentInParent<ICatchable>();
            if (catchable == null || !catchable.CanCatch || !catchable.TryCatch())
            {
                HuntShotMissed?.Invoke(aimRay);
                return;
            }

            HuntShotHit?.Invoke(hit.point, hit.collider.gameObject);
        }

        private bool TryCastForTarget(Ray aimRay, out RaycastHit hit)
        {
            if (Config == null)
            {
                hit = default;
                return false;
            }

            float range = Config.Range;
            float aimRadius = Config.AimRadius;
            LayerMask hitMask = Config.HitMask;

            if (aimRadius > 0f)
            {
                return Physics.SphereCast(aimRay, aimRadius, out hit, range, hitMask, QueryTriggerInteraction.Collide);
            }

            return Physics.Raycast(aimRay, out hit, range, hitMask, QueryTriggerInteraction.Collide);
        }
    }
}
