using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Design configuration for the Level 4 octopus encounter, ink splash values, and zone movement.
    /// </summary>
    [CreateAssetMenu(menuName = "Coral Rescue/Octopus Config", fileName = "OctopusConfig")]
    public sealed class OctopusConfig : ScriptableObject
    {
        [Header("Obligation")]
        [SerializeField] private ObligationConfig _obligation;

        [Header("Spawn & Zone Movement")]
        [SerializeField] private Vector3 _spawnPosition = new Vector3(0f, 1f, -12f);
        [SerializeField] private Vector2 _zoneMoveAmplitude = new Vector2(3f, 0.75f);
        [SerializeField, Min(0f)] private float _zoneMoveSpeed = 1.2f;
        [SerializeField, Min(0f)] private float _warningDuration = 1.5f;
        [SerializeField, Min(0f)] private float _invulnerableDuration = 1.5f;

        [Header("Ink Projectile & Screen Threshold")]
        [SerializeField, Min(1)] private int _inkPoolSize = 8;
        [SerializeField, Range(0f, 1f)] private float _inkAmountPerBall = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _retreatInkThreshold = 0.35f;
        [SerializeField, Min(0.05f)] private float _inkHitDistance = 0.75f;
        [SerializeField] private Vector3 _inkSpawnOffset = new Vector3(0f, 0.25f, 0.75f);
        [SerializeField, Min(0.1f)] private float _inkBallMaxLifetime = 10f;

        public ObligationConfig Obligation => _obligation;
        public Vector3 SpawnPosition => _spawnPosition;
        public Vector2 ZoneMoveAmplitude => new Vector2(Mathf.Max(0f, _zoneMoveAmplitude.x), Mathf.Max(0f, _zoneMoveAmplitude.y));
        public float ZoneMoveSpeed => Mathf.Max(0f, _zoneMoveSpeed);
        public float WarningDuration => Mathf.Max(0f, _warningDuration);
        public float InvulnerableDuration => Mathf.Max(0f, _invulnerableDuration);
        public int InkPoolSize => Mathf.Max(1, _inkPoolSize);
        public float InkAmountPerBall => Mathf.Clamp01(_inkAmountPerBall);
        public float RetreatInkThreshold => Mathf.Clamp01(_retreatInkThreshold);
        public float InkHitDistance => Mathf.Max(0.05f, _inkHitDistance);
        public Vector3 InkSpawnOffset => _inkSpawnOffset;
        public float InkBallMaxLifetime => Mathf.Max(0.1f, _inkBallMaxLifetime);
    }
}
