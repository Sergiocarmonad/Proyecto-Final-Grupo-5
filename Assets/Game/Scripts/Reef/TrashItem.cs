using System;
using Game.Weapons;
using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Collectible falling trash obligation. Inactive = falling, Pending = landed on floor, Resolved = collected.
    /// </summary>
    public sealed class TrashItem : ObligationBase, ICollectible
    {
        [SerializeField] private float _defaultFallSpeed = 1.5f;
        [SerializeField] private LayerMask _floorMask = ~0;
        [SerializeField] private float _fallbackFloorY;
        [SerializeField] private float _surfaceOffset = 0.05f;
        [SerializeField] private float _raycastStartOffset = 0.1f;

        private float _fallSpeed;

        /// <summary>
        /// Raised when the trash hits the seabed, passing (item, oneTimePenaltyApplied).
        /// </summary>
        public event Action<TrashItem, float> Landed;

        /// <summary>
        /// Raised when the trash is gathered by the collect weapon, passing the item.
        /// </summary>
        public event Action<TrashItem> Collected;

        public bool CanCollect => State == ObligationState.Inactive || State == ObligationState.Pending;

        private void Awake()
        {
            if (_fallSpeed <= 0f)
            {
                _fallSpeed = _defaultFallSpeed;
            }
        }

        protected override void Update()
        {
            if (State == ObligationState.Inactive)
            {
                UpdateFalling();
                return;
            }

            base.Update();
        }

        /// <summary>
        /// Configures this pooled instance for a new spawn in the falling (Inactive) state.
        /// </summary>
        public void InitializeTrash(ReefHealth reefHealth, TrashTypeConfig typeConfig, LayerMask floorMask, float fallbackFloorY)
        {
            ResetState();
            _floorMask = floorMask;
            _fallbackFloorY = fallbackFloorY;
            _fallSpeed = typeConfig != null ? typeConfig.FallSpeed : _defaultFallSpeed;
            ObligationConfig obligationConfig = typeConfig != null ? typeConfig.ObligationConfig : null;
            SetDependencies(reefHealth, obligationConfig);
        }

        /// <summary>
        /// Resets obligation and movement state so the instance can be reused from a pool.
        /// </summary>
        public void ResetState()
        {
            ResetObligationState();
            _fallSpeed = _defaultFallSpeed;
        }

        public bool TryCollect()
        {
            if (!CanCollect)
            {
                return false;
            }

            if (State == ObligationState.Inactive)
            {
                MarkResolved();
            }
            else
            {
                Resolve();
            }

            Collected?.Invoke(this);
            gameObject.SetActive(false);
            return true;
        }

        private void UpdateFalling()
        {
            float fallStep = Mathf.Max(0f, _fallSpeed) * Time.deltaTime;
            if (fallStep <= 0f)
            {
                return;
            }

            Vector3 position = transform.position;
            Vector3 rayOrigin = position + Vector3.up * _raycastStartOffset;
            float rayDistance = fallStep + _raycastStartOffset;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, _floorMask, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
            {
                position.y = hit.point.y + _surfaceOffset;
                transform.position = position;
                Land();
                return;
            }

            position.y -= fallStep;
            float floorY = _fallbackFloorY + _surfaceOffset;
            if (position.y <= floorY)
            {
                position.y = floorY;
                transform.position = position;
                Land();
                return;
            }

            transform.position = position;
        }

        private void Land()
        {
            if (State != ObligationState.Inactive)
            {
                return;
            }

            float appliedPenalty = ApplyOneTimePenalty();
            BeginPending();
            Landed?.Invoke(this, appliedPenalty);
        }
    }
}
