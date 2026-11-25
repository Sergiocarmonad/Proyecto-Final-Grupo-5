using System;
using System.Collections.Generic;
using Game.Reef;
using UnityEngine;

namespace Game.Flow
{
    [Serializable]
    public sealed class TrashSpawnEntry
    {
        [SerializeField] private TrashTypeConfig _trashType;
        [SerializeField] private float _weight = 1f;

        public TrashTypeConfig TrashType => _trashType;
        public float Weight => Mathf.Max(0f, _weight);
    }

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

        [Header("Trash Settings")]
        [SerializeField] private TrashSpawnEntry[] _trashTypes;
        [SerializeField] private float _initialTrashDelay = 1.5f;
        [SerializeField] private float _trashSpawnIntervalStart = 4f;
        [SerializeField] private float _trashSpawnIntervalEnd = 2f;
        [SerializeField] private int _maxActiveTrash;

        [Header("Oil Spill Settings")]
        [SerializeField] private float _initialOilSpillDelay = 3f;
        [SerializeField] private float _oilSpillIntervalStart = 12f;
        [SerializeField] private float _oilSpillIntervalEnd = 6f;
        [SerializeField] private float _oilSpillIntensity = 0.25f;

        public float DurationSeconds => Mathf.Max(0f, _durationSeconds);
        public float MaxHealth => _maxHealth;
        public bool TrashEnabled => _trashEnabled;
        public bool CleaningEnabled => _cleaningEnabled;
        public bool OilSpillsEnabled => _oilSpillsEnabled;
        public bool FishEnabled => _fishEnabled;
        public bool OctopusEnabled => _octopusEnabled;
        public IReadOnlyList<TrashSpawnEntry> TrashTypes => _trashTypes;
        public float InitialTrashDelay => Mathf.Max(0f, _initialTrashDelay);
        public float TrashSpawnIntervalStart => Mathf.Max(0.1f, _trashSpawnIntervalStart);
        public float TrashSpawnIntervalEnd => Mathf.Max(0.1f, _trashSpawnIntervalEnd);
        public int MaxActiveTrash => Mathf.Max(0, _maxActiveTrash);
        public float InitialOilSpillDelay => Mathf.Max(0f, _initialOilSpillDelay);
        public float OilSpillIntervalStart => Mathf.Max(0.1f, _oilSpillIntervalStart);
        public float OilSpillIntervalEnd => Mathf.Max(0.1f, _oilSpillIntervalEnd);
        public float OilSpillIntensity => Mathf.Max(0.01f, _oilSpillIntensity);
    }
}
