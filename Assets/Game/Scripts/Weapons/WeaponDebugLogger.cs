using UnityEngine;

namespace Game.Weapons
{
    // Temporary debug script — delete after validating weapon events in Unity.
    /// <summary>
    /// Temporary logger that prints WeaponBase and CollectWeapon events to the Unity console.
    /// </summary>
    public sealed class WeaponDebugLogger : MonoBehaviour
    {
        [SerializeField] private WeaponBase _weapon;
        [SerializeField] private CollectWeapon _collectWeapon;

        private WeaponBase _subscribedWeapon;

        private void OnEnable()
        {
            _subscribedWeapon = _weapon != null ? _weapon : _collectWeapon;
            if (_subscribedWeapon != null)
            {
                _subscribedWeapon.UseExecuted += HandleUseExecuted;
                _subscribedWeapon.AmmoChanged += HandleAmmoChanged;
                _subscribedWeapon.ReloadStarted += HandleReloadStarted;
                _subscribedWeapon.ReloadFinished += HandleReloadFinished;
                _subscribedWeapon.EmptyTriggered += HandleEmptyTriggered;
            }

            if (_collectWeapon != null)
            {
                _collectWeapon.CollectSucceeded += HandleCollectSucceeded;
                _collectWeapon.CollectMissed += HandleCollectMissed;
            }
        }

        private void OnDisable()
        {
            if (_subscribedWeapon != null)
            {
                _subscribedWeapon.UseExecuted -= HandleUseExecuted;
                _subscribedWeapon.AmmoChanged -= HandleAmmoChanged;
                _subscribedWeapon.ReloadStarted -= HandleReloadStarted;
                _subscribedWeapon.ReloadFinished -= HandleReloadFinished;
                _subscribedWeapon.EmptyTriggered -= HandleEmptyTriggered;
                _subscribedWeapon = null;
            }

            if (_collectWeapon != null)
            {
                _collectWeapon.CollectSucceeded -= HandleCollectSucceeded;
                _collectWeapon.CollectMissed -= HandleCollectMissed;
            }
        }

        private void HandleUseExecuted(Ray aimRay)
        {
            Debug.Log($"[WeaponDebug] UseExecuted origin={aimRay.origin} dir={aimRay.direction}", this);
        }

        private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
        {
            Debug.Log($"[WeaponDebug] AmmoChanged {currentAmmo}/{maxAmmo}", this);
        }

        private void HandleReloadStarted(float duration)
        {
            Debug.Log($"[WeaponDebug] ReloadStarted duration={duration:0.00}s", this);
        }

        private void HandleReloadFinished()
        {
            Debug.Log("[WeaponDebug] ReloadFinished", this);
        }

        private void HandleEmptyTriggered()
        {
            Debug.Log("[WeaponDebug] EmptyTriggered", this);
        }

        private void HandleCollectSucceeded(Vector3 hitPoint, GameObject target)
        {
            string targetName = target != null ? target.name : "null";
            Debug.Log($"[WeaponDebug] CollectSucceeded target={targetName} point={hitPoint}", this);
        }

        private void HandleCollectMissed(Ray aimRay)
        {
            Debug.Log($"[WeaponDebug] CollectMissed origin={aimRay.origin} dir={aimRay.direction}", this);
        }
    }
}
