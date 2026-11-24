using UnityEngine;

namespace Game.Weapons
{
    // Temporary test script — delete after validating CollectWeapon in Unity.
    /// <summary>
    /// Minimal collectible target for testing CollectWeapon before the Trash module is implemented.
    /// </summary>
    public sealed class TestCollectible : MonoBehaviour, ICollectible
    {
        [SerializeField] private bool _deactivateOnCollect = true;

        private bool _isCollected;

        public bool CanCollect => !_isCollected;

        public bool TryCollect()
        {
            if (_isCollected)
            {
                return false;
            }

            _isCollected = true;
            Debug.Log($"TestCollectible collected: {name}", this);

            if (_deactivateOnCollect)
            {
                gameObject.SetActive(false);
            }

            return true;
        }
    }
}
