using System;
using Game.Weapons;
using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Pooled ink projectile that moves toward the player target using scaled time and can be shot down by HuntingWeapon.
    /// </summary>
    public sealed class InkBall : MonoBehaviour, ICatchable
    {
        private Transform _target;
        private float _speed;
        private float _hitDistanceSqr;
        private float _remainingLifetime;
        private bool _isActive;
        private Action<InkBall> _onReachedPlayer;
        private Action<InkBall> _onIntercepted;

        public bool CanCatch => _isActive;
        public bool IsActiveProjectile => _isActive;

        /// <summary>
        /// Activates the pooled ink ball and launches it toward the specified player target.
        /// </summary>
        public void Launch(
            Vector3 startPosition,
            Transform target,
            float speed,
            float hitDistance,
            float maxLifetime,
            Action<InkBall> onReachedPlayer,
            Action<InkBall> onIntercepted)
        {
            transform.position = startPosition;
            _target = target;
            _speed = Mathf.Max(0.1f, speed);
            float clampedDistance = Mathf.Max(0.05f, hitDistance);
            _hitDistanceSqr = clampedDistance * clampedDistance;
            _remainingLifetime = Mathf.Max(0.1f, maxLifetime);
            _onReachedPlayer = onReachedPlayer;
            _onIntercepted = onIntercepted;
            _isActive = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            if (_target == null)
            {
                ResetState();
                return;
            }

            Vector3 targetPosition = _target.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.deltaTime);

            if ((transform.position - targetPosition).sqrMagnitude <= _hitDistanceSqr)
            {
                Action<InkBall> callback = _onReachedPlayer;
                ResetState();
                callback?.Invoke(this);
                return;
            }

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                Action<InkBall> callback = _onReachedPlayer;
                ResetState();
                callback?.Invoke(this);
            }
        }

        /// <summary>
        /// Intercepts the ink ball when hit by HuntingWeapon and returns control to the pool owner.
        /// </summary>
        public bool TryCatch()
        {
            if (!CanCatch)
            {
                return false;
            }

            Action<InkBall> callback = _onIntercepted;
            ResetState();
            callback?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Resets projectile state and deactivates the object for pool reuse without destroying it.
        /// </summary>
        public void ResetState()
        {
            _isActive = false;
            _target = null;
            _onReachedPlayer = null;
            _onIntercepted = null;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
