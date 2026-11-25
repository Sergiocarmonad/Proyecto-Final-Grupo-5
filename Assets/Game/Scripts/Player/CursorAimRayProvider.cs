using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// Exposes the world-space aiming ray originating from the current cursor position.
    /// </summary>
    public sealed class CursorAimRayProvider : MonoBehaviour
    {
        [SerializeField] private Camera _targetCamera;

        /// <summary>
        /// Current aiming ray from the cursor, or default if no camera or mouse is available.
        /// </summary>
        public Ray CurrentAimRay => TryGetAimRay(out Ray aimRay) ? aimRay : default;

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = GetComponent<Camera>();
            }
        }

        /// <summary>
        /// Computes the aiming ray from the current mouse screen position through the cached camera.
        /// </summary>
        public bool TryGetAimRay(out Ray aimRay)
        {
            Mouse mouse = Mouse.current;
            if (_targetCamera == null || mouse == null)
            {
                aimRay = default;
                return false;
            }

            Vector2 screenPosition = mouse.position.ReadValue();
            aimRay = _targetCamera.ScreenPointToRay(screenPosition);
            return true;
        }
    }
}
