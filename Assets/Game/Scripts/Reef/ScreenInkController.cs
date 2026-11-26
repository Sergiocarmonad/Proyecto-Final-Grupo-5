using System;
using UnityEngine;

namespace Game.Reef
{
    /// <summary>
    /// Tracks normalized screen ink coverage [0..1] and emits splash, progress, and clean events.
    /// </summary>
    public sealed class ScreenInkController : MonoBehaviour
    {
        /// <summary>
        /// Raised when ink hits the player screen.
        /// </summary>
        public event Action InkSplashed;

        /// <summary>
        /// Raised whenever the normalized screen ink level [0..1] changes.
        /// </summary>
        public event Action<float> InkChanged;

        /// <summary>
        /// Raised when player cleaning reduces screen ink to zero.
        /// </summary>
        public event Action InkCleaned;

        public float InkLevel { get; private set; }
        public bool HasInk => InkLevel > 0f;

        /// <summary>
        /// Adds normalized ink to the screen, clamped to [0..1].
        /// </summary>
        public bool AddInk(float amount)
        {
            if (amount <= 0f)
            {
                return false;
            }

            float nextLevel = Mathf.Clamp01(InkLevel + amount);
            bool changed = !Mathf.Approximately(nextLevel, InkLevel);
            InkLevel = nextLevel;

            InkSplashed?.Invoke();

            if (changed)
            {
                InkChanged?.Invoke(InkLevel);
            }

            return true;
        }

        /// <summary>
        /// Removes normalized ink from the screen, emitting InkCleaned once InkLevel reaches zero.
        /// </summary>
        public bool CleanInk(float amount)
        {
            if (amount <= 0f || !HasInk)
            {
                return false;
            }

            float nextLevel = Mathf.Clamp01(InkLevel - amount);
            if (Mathf.Approximately(nextLevel, 0f))
            {
                nextLevel = 0f;
            }

            InkLevel = nextLevel;
            InkChanged?.Invoke(InkLevel);

            if (InkLevel <= 0f)
            {
                InkCleaned?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// Resets screen ink to zero without triggering player-clean completion feedback.
        /// </summary>
        public void ResetInk()
        {
            if (InkLevel <= 0f)
            {
                InkLevel = 0f;
                return;
            }

            InkLevel = 0f;
            InkChanged?.Invoke(0f);
        }
    }
}
