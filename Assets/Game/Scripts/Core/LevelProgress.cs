using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Stores the active 1-based level index across Gameplay scene reloads.
    /// </summary>
    public static class LevelProgress
    {
        public const int FirstLevelIndex = 1;

        public static int CurrentLevelIndex { get; private set; } = FirstLevelIndex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeOnSubsystemRegistration()
        {
            CurrentLevelIndex = FirstLevelIndex;
        }

        /// <summary>
        /// Sets the active 1-based level index, clamped to at least 1.
        /// </summary>
        public static void SetLevel(int levelIndex)
        {
            CurrentLevelIndex = Mathf.Max(FirstLevelIndex, levelIndex);
        }

        /// <summary>
        /// Advances the current level index by one.
        /// </summary>
        public static void AdvanceLevel()
        {
            CurrentLevelIndex++;
        }

        /// <summary>
        /// Resets progression back to the first level.
        /// </summary>
        public static void Reset()
        {
            CurrentLevelIndex = FirstLevelIndex;
        }
    }
}
