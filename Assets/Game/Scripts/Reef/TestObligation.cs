using UnityEngine;

namespace Game.Reef
{
    // Temporary test component for verifying the base obligation pattern; delete after validation.
    public sealed class TestObligation : ObligationBase
    {
        [SerializeField] private bool _logEvents = true;

        private void OnEnable()
        {
            StateChanged += HandleStateChanged;
            OneTimePenaltyApplied += HandleOneTimePenaltyApplied;
        }

        private void OnDisable()
        {
            StateChanged -= HandleStateChanged;
            OneTimePenaltyApplied -= HandleOneTimePenaltyApplied;
        }

        [ContextMenu("Test/Begin Pending")]
        public void TestBeginPending()
        {
            BeginPending();
        }

        [ContextMenu("Test/Fail (Apply Penalty)")]
        public void TestFail()
        {
            Fail();
        }

        [ContextMenu("Test/Resolve")]
        public void TestResolve()
        {
            Resolve();
        }

        private void HandleStateChanged(ObligationBase obligation, ObligationState previousState, ObligationState newState)
        {
            if (_logEvents)
            {
                Debug.Log($"[TestObligation] State changed: {previousState} -> {newState}", this);
            }
        }

        private void HandleOneTimePenaltyApplied(ObligationBase obligation, float penaltyAmount)
        {
            if (_logEvents)
            {
                Debug.Log($"[TestObligation] One-time penalty applied: {penaltyAmount}", this);
            }
        }
    }
}
