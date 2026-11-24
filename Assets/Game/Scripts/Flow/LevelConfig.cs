using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// Declarative level settings defining survival duration, maximum health, and active obligations.
    /// </summary>
    [CreateAssetMenu(menuName = "Coral Rescue/Level Config", fileName = "LevelConfig")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("Core")]
        [SerializeField] private float _durationSeconds = 120f;
        [SerializeField] private float _maxHealth = 100f;

        [Header("Active Obligations")]
        [SerializeField] private bool _trashEnabled = true;
        [SerializeField] private bool _cleaningEnabled;
        [SerializeField] private bool _oilSpillsEnabled;
        [SerializeField] private bool _fishEnabled;
        [SerializeField] private bool _octopusEnabled;

        public float DurationSeconds => Mathf.Max(0f, _durationSeconds);
        public float MaxHealth => _maxHealth;
        public bool TrashEnabled => _trashEnabled;
        public bool CleaningEnabled => _cleaningEnabled;
        public bool OilSpillsEnabled => _oilSpillsEnabled;
        public bool FishEnabled => _fishEnabled;
        public bool OctopusEnabled => _octopusEnabled;
    }
}
