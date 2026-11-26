using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Configurable penalty and continuous damage settings for a punishable obligation.
    /// </summary>
    [CreateAssetMenu(menuName = "Coral Rescue/Obligation Config", fileName = "ObligationConfig")]
    public sealed class ObligationConfig : ScriptableObject
    {
        [Header("Penalties")]
        [SerializeField, Min(0f)] private float _oneTimePenalty;
        [SerializeField, Min(0f)] private float _damagePerSecond;

        [Header("Optional Growth Curve")]
        [SerializeField] private bool _useDamageGrowthCurve;
        [SerializeField] private AnimationCurve _damageGrowthCurve = AnimationCurve.Linear(0f, 1f, 30f, 2f);

        [Header("Failure Behavior")]
        [SerializeField] private bool _continueDamageWhenFailed;

        public float OneTimePenalty => Mathf.Max(0f, _oneTimePenalty);
        public float DamagePerSecond => Mathf.Max(0f, _damagePerSecond);
        public bool UseDamageGrowthCurve => _useDamageGrowthCurve;
        public bool ContinueDamageWhenFailed => _continueDamageWhenFailed;

        /// <summary>
        /// Evaluates damage per second for the given elapsed pending duration in seconds.
        /// </summary>
        public float EvaluateDamagePerSecond(float elapsedPendingSeconds)
        {
            float baseDamage = DamagePerSecond;
            if (baseDamage <= 0f)
            {
                return 0f;
            }

            if (!_useDamageGrowthCurve || _damageGrowthCurve == null)
            {
                return baseDamage;
            }

            float multiplier = Mathf.Max(0f, _damageGrowthCurve.Evaluate(Mathf.Max(0f, elapsedPendingSeconds)));
            return baseDamage * multiplier;
        }
    }
}
