using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Configurable swim behavior and obligation damage settings for a harmful fish type.
    /// </summary>
    [CreateAssetMenu(menuName = "Coral Rescue/Fish Config", fileName = "FishConfig")]
    public sealed class FishConfig : ScriptableObject
    {
        [Header("Obligation")]
        [SerializeField] private ObligationConfig _obligation;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float _swimSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float _turnSpeed = 4f;
        [SerializeField, Min(0.5f)] private float _patrolRadius = 6f;
        [SerializeField] private float _minHeightOffset = -1.5f;
        [SerializeField] private float _maxHeightOffset = 1.5f;
        [SerializeField, Min(0.05f)] private float _waypointReachDistance = 0.5f;

        public ObligationConfig Obligation => _obligation;
        public float SwimSpeed => Mathf.Max(0.1f, _swimSpeed);
        public float TurnSpeed => Mathf.Max(0.1f, _turnSpeed);
        public float PatrolRadius => Mathf.Max(0.5f, _patrolRadius);
        public float MinHeightOffset => Mathf.Min(_minHeightOffset, _maxHeightOffset);
        public float MaxHeightOffset => Mathf.Max(_minHeightOffset, _maxHeightOffset);
        public float WaypointReachDistance => Mathf.Max(0.05f, _waypointReachDistance);
    }
}
