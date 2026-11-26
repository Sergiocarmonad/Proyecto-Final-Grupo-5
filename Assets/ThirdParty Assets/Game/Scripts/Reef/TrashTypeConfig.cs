using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Configuration for a falling trash variant, linking its prefab, obligation penalties, and fall speed.
    /// </summary>
    [CreateAssetMenu(menuName = "Coral Rescue/Trash Type Config", fileName = "TrashTypeConfig")]
    public sealed class TrashTypeConfig : ScriptableObject
    {
        [SerializeField] private TrashItem _prefab;
        [SerializeField] private ObligationConfig _obligationConfig;
        [SerializeField] private float _fallSpeed = 1.5f;

        public TrashItem Prefab => _prefab;
        public ObligationConfig ObligationConfig => _obligationConfig;
        public float FallSpeed => Mathf.Max(0.05f, _fallSpeed);
    }
}
