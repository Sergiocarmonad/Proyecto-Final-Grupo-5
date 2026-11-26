using System;
using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Tracks the shared player and reef health through a single damage entry point.
    /// </summary>
    public sealed class ReefHealth : MonoBehaviour
    {
        [SerializeField] private float _defaultMaxHealth = 100f;

        /// <summary>
        /// Raised whenever health is initialized or changes, passing (currentHealth, maxHealth).
        /// </summary>
        public event Action<float, float> HealthChanged;

        /// <summary>
        /// Raised once when health reaches zero.
        /// </summary>
        public event Action Depleted;

        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }
        public float NormalizedHealth => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        public bool IsDepleted => CurrentHealth <= 0f;
        public bool IsContinuousDamagePaused { get; private set; }
        public bool IsDamageBlocked { get; private set; }

        private void Awake()
        {
            if (MaxHealth <= 0f)
            {
                ResetHealth(_defaultMaxHealth);
            }
        }

        /// <summary>
        /// Resets health to the given maximum value, falling back to the serialized default if invalid.
        /// </summary>
        public void ResetHealth(float maxHealth)
        {
            float resolvedMax = maxHealth > 0f ? maxHealth : _defaultMaxHealth;
            MaxHealth = Mathf.Max(0f, resolvedMax);
            CurrentHealth = MaxHealth;
            IsContinuousDamagePaused = false;
            IsDamageBlocked = false;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// Pauses or resumes continuous damage ticks from obligations without affecting one-time penalties.
        /// </summary>
        public void SetContinuousDamagePaused(bool paused)
        {
            IsContinuousDamagePaused = paused;
        }

        /// <summary>
        /// Blocks or unblocks all incoming damage (continuous ticks and one-time penalties).
        /// </summary>
        public void SetDamageBlocked(bool blocked)
        {
            IsDamageBlocked = blocked;
        }

        /// <summary>
        /// Single damage entry point for all penalties and hazards.
        /// </summary>
        public void ApplyDamage(float amount)
        {
            if (IsDamageBlocked || !(amount > 0f) || IsDepleted)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (IsDepleted)
            {
                Depleted?.Invoke();
            }
        }
    }
}
