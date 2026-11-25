using Game.Reef;
using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// Temporary debug logger that logs level flow and reef health events and exposes test damage context actions.
    /// </summary>
    public sealed class LevelFlowDebugLogger : MonoBehaviour
    {
        private const float MediumDamageAmount = 50f;
        private const float LethalDamageAmount = 200f;

        [SerializeField] private LevelFlowController _levelFlowController;
        [SerializeField] private ReefHealth _reefHealth;

        private void OnEnable()
        {
            if (_levelFlowController != null)
            {
                _levelFlowController.LevelStarted += HandleLevelStarted;
                _levelFlowController.LevelWon += HandleLevelWon;
                _levelFlowController.LevelLost += HandleLevelLost;
            }

            if (_reefHealth != null)
            {
                _reefHealth.HealthChanged += HandleHealthChanged;
                _reefHealth.Depleted += HandleDepleted;
            }
        }

        private void OnDisable()
        {
            if (_levelFlowController != null)
            {
                _levelFlowController.LevelStarted -= HandleLevelStarted;
                _levelFlowController.LevelWon -= HandleLevelWon;
                _levelFlowController.LevelLost -= HandleLevelLost;
            }

            if (_reefHealth != null)
            {
                _reefHealth.HealthChanged -= HandleHealthChanged;
                _reefHealth.Depleted -= HandleDepleted;
            }
        }

        [ContextMenu("Damage 50")]
        private void ApplyDamage50()
        {
            if (_reefHealth == null)
            {
                Debug.LogWarning("ReefHealth reference is missing on LevelFlowDebugLogger.", this);
                return;
            }

            _reefHealth.ApplyDamage(MediumDamageAmount);
        }

        [ContextMenu("Damage 200")]
        private void ApplyDamage200()
        {
            if (_reefHealth == null)
            {
                Debug.LogWarning("ReefHealth reference is missing on LevelFlowDebugLogger.", this);
                return;
            }

            _reefHealth.ApplyDamage(LethalDamageAmount);
        }

        private void HandleLevelStarted(LevelConfig config)
        {
            string levelName = config != null ? config.name : "Unknown";
            Debug.Log($"[LevelFlow] LevelStarted: {levelName}", this);
        }

        private void HandleLevelWon(LevelConfig config)
        {
            string levelName = config != null ? config.name : "Unknown";
            Debug.Log($"[LevelFlow] LevelWon: {levelName}", this);
        }

        private void HandleLevelLost(LevelConfig config)
        {
            string levelName = config != null ? config.name : "Unknown";
            Debug.Log($"[LevelFlow] LevelLost: {levelName}", this);
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            Debug.Log($"[ReefHealth] HealthChanged: {currentHealth}/{maxHealth}", this);
        }

        private void HandleDepleted()
        {
            Debug.Log("[ReefHealth] Depleted", this);
        }
    }
}
