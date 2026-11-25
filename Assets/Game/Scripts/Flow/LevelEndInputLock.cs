using Game.Player;
using Game.Weapons;
using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// Locks gameplay input, camera edge rotation, and the weapon wheel when a level ends,
    /// and restores them when a level starts.
    /// </summary>
    public sealed class LevelEndInputLock : MonoBehaviour
    {
        [SerializeField] private LevelFlowController _levelFlowController;
        [SerializeField] private PlayerWeaponController _weaponController;
        [SerializeField] private WeaponWheelController _weaponWheelController;
        [SerializeField] private EdgeLookCameraController _edgeLookCamera;

        private void OnEnable()
        {
            if (_levelFlowController == null)
            {
                return;
            }

            _levelFlowController.LevelStarted += HandleLevelStarted;
            _levelFlowController.LevelWon += HandleLevelEnded;
            _levelFlowController.LevelLost += HandleLevelEnded;
        }

        private void OnDisable()
        {
            if (_levelFlowController == null)
            {
                return;
            }

            _levelFlowController.LevelStarted -= HandleLevelStarted;
            _levelFlowController.LevelWon -= HandleLevelEnded;
            _levelFlowController.LevelLost -= HandleLevelEnded;
        }

        private void HandleLevelStarted(LevelConfig _)
        {
            SetGameplayLocked(false);
        }

        private void HandleLevelEnded(LevelConfig _)
        {
            SetGameplayLocked(true);
        }

        private void SetGameplayLocked(bool locked)
        {
            // Disable the wheel before locking input/camera so its OnDisable cleanup cannot re-enable them.
            if (_weaponWheelController != null)
            {
                _weaponWheelController.enabled = !locked;
            }

            if (_weaponController != null)
            {
                _weaponController.SetInputBlocked(locked);
            }

            if (_edgeLookCamera != null)
            {
                _edgeLookCamera.SetRotationEnabled(!locked);
            }

            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }
}
