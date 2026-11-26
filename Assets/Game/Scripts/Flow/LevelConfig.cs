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

    [Serializable]
    public sealed class FishSpawnEntry
    {
        [SerializeField] private FishConfig _fishConfig;
        [SerializeField] private float _weight = 1f;

        public FishConfig FishConfig => _fishConfig;
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

        [Header("Fish Settings")]
        [SerializeField] private FishSpawnEntry[] _fishTypes;
        [SerializeField] private float _initialFishDelay = 4f;
        [SerializeField] private float _fishSpawnIntervalStart = 10f;
        [SerializeField] private float _fishSpawnIntervalEnd = 5f;
        [SerializeField] private int _maxActiveFish = 3;

        [Header("Octopus Settings")]
        [SerializeField, Min(0f)] private float _initialOctopusDelay = 20f;
        [SerializeField, Min(0f)] private float _octopusPauseDuration = 25f;
        [SerializeField, Min(1)] private int _octopusMaxHits = 3;
        [SerializeField, Min(0.1f)] private float _inkSpawnInterval = 4f;
        [SerializeField, Min(0.1f)] private float _inkBallSpeed = 8f;
        [SerializeField, Min(0f)] private float _octopusLaughDelay = 1.5f;

        [Header("Victory Bonus")]
        [SerializeField, Min(0f)] private float _bonusPointsPerHealth = 10f;
        [SerializeField, Min(0f)] private float _bonusPointsPerTrash = 50f;

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
        public IReadOnlyList<FishSpawnEntry> FishTypes => _fishTypes;
        public float InitialFishDelay => Mathf.Max(0f, _initialFishDelay);
        public float FishSpawnIntervalStart => Mathf.Max(0.1f, _fishSpawnIntervalStart);
        public float FishSpawnIntervalEnd => Mathf.Max(0.1f, _fishSpawnIntervalEnd);
        public int MaxActiveFish => Mathf.Max(0, _maxActiveFish);
        public float InitialOctopusDelay => Mathf.Max(0f, _initialOctopusDelay);
        public float OctopusPauseDuration => Mathf.Max(0f, _octopusPauseDuration);
        public int OctopusMaxHits => Mathf.Max(1, _octopusMaxHits);
        public float InkSpawnInterval => Mathf.Max(0.1f, _inkSpawnInterval);
        public float InkBallSpeed => Mathf.Max(0.1f, _inkBallSpeed);
        public float OctopusLaughDelay => Mathf.Max(0f, _octopusLaughDelay);
        public float BonusPointsPerHealth => Mathf.Max(0f, _bonusPointsPerHealth);
        public float BonusPointsPerTrash => Mathf.Max(0f, _bonusPointsPerTrash);
    }
}
