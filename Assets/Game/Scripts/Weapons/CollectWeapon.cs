using System;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Collect tool that casts along the aiming ray to gather ICollectible targets using cooldown only.
    /// </summary>
    public sealed class CollectWeapon : WeaponBase
    {
        /// <summary>
        /// Raised when a collectible target is gathered, passing (hitPoint, targetObject).
        /// </summary>
        public event Action<Vector3, GameObject> CollectSucceeded;

        /// <summary>
        /// Raised when the collect ray finds no eligible target, passing the aiming ray.
        /// </summary>
        public event Action<Ray> CollectMissed;

        protected override void ExecuteUse(Ray aimRay)
        {
            if (!TryCastForTarget(aimRay, out RaycastHit hit))
            {
                CollectMissed?.Invoke(aimRay);
                return;
            }

            ICollectible collectible = hit.collider.GetComponentInParent<ICollectible>();
            if (collectible == null || !collectible.CanCollect || !collectible.TryCollect())
            {
                CollectMissed?.Invoke(aimRay);
                return;
            }

            CollectSucceeded?.Invoke(hit.point, hit.collider.gameObject);
        }

        private bool TryCastForTarget(Ray aimRay, out RaycastHit hit)
        {
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
