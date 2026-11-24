using System;
using UnityEngine;

namespace Game.Reef
{
    public enum ObligationState
    {
        Inactive = 0,
        Pending = 1,
        Resolved = 2,
        Failed = 3
    }

    /// <summary>
    /// Base state machine and damage loop for obligations that penalize reef health until resolved.
    /// </summary>
    public abstract class ObligationBase : MonoBehaviour
    {
        [SerializeField] private ObligationConfig _config;
        [SerializeField] private ReefHealth _reefHealth;

        /// <summary>
        /// Raised when the obligation transitions between states, passing (obligation, previousState, newState).
        /// </summary>
        public event Action<ObligationBase, ObligationState, ObligationState> StateChanged;

        /// <summary>
        /// Raised when a non-zero one-time penalty is applied on failure, passing (obligation, penaltyAmount).
        /// </summary>
        public event Action<ObligationBase, float> OneTimePenaltyApplied;

        public ObligationConfig Config => _config;
        public ReefHealth ReefHealth => _reefHealth;
        public ObligationState State { get; private set; } = ObligationState.Inactive;
        public float PendingElapsedTime { get; private set; }
        public float CurrentDamagePerSecond { get; private set; }

        protected virtual void Update()
        {
            if (State != ObligationState.Pending || _config == null)
            {
                CurrentDamagePerSecond = 0f;
                return;
            }

            PendingElapsedTime += Time.deltaTime;
            float dps = _config.EvaluateDamagePerSecond(PendingElapsedTime);
            CurrentDamagePerSecond = dps;

            if (dps <= 0f || _reefHealth == null || _reefHealth.IsDepleted)
            {
                return;
            }

            _reefHealth.ApplyDamage(dps * Time.deltaTime);
        }

        /// <summary>
        /// Injects runtime dependencies for dynamically spawned obligations.
        /// </summary>
        public void Initialize(ReefHealth reefHealth, ObligationConfig config = null)
        {
            _reefHealth = reefHealth;
            if (config != null)
            {
                _config = config;
            }

            BeginPending();
        }

        /// <summary>
        /// Transitions an Inactive obligation to Pending and resets its pending timer. Ignored if not Inactive.
        /// </summary>
        public void BeginPending()
        {
            if (State != ObligationState.Inactive)
            {
                return;
            }

            PendingElapsedTime = 0f;
            CurrentDamagePerSecond = 0f;
            TransitionTo(ObligationState.Pending);
            OnBecamePending();
        }

        /// <summary>
        /// Resolves a Pending obligation and stops continuous damage. Ignored if not Pending.
        /// </summary>
        public void Resolve()
        {
            if (State != ObligationState.Pending)
            {
                return;
            }

            CurrentDamagePerSecond = 0f;
            TransitionTo(ObligationState.Resolved);
            OnResolved();
        }

        /// <summary>
        /// Marks a Pending obligation as Failed and applies its configured one-time penalty. Ignored if not Pending.
        /// </summary>
        public void Fail()
        {
            if (State != ObligationState.Pending)
            {
                return;
            }

            CurrentDamagePerSecond = 0f;
            TransitionTo(ObligationState.Failed);

            float penalty = _config != null ? _config.OneTimePenalty : 0f;
            if (penalty > 0f && _reefHealth != null && !_reefHealth.IsDepleted)
            {
                _reefHealth.ApplyDamage(penalty);
                OneTimePenaltyApplied?.Invoke(this, penalty);
            }

            OnFailed();
        }

        protected virtual void OnBecamePending() { }
        protected virtual void OnResolved() { }
        protected virtual void OnFailed() { }

        private void TransitionTo(ObligationState nextState)
        {
            ObligationState previousState = State;
            if (previousState == nextState)
            {
                return;
            }

            State = nextState;
            StateChanged?.Invoke(this, previousState, nextState);
        }
    }
}
