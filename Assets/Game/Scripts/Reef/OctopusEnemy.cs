using System;
using System.Collections.Generic;
using Game.Radar;
using Game.Weapons;
using UnityEngine;

namespace Game.Reef
{
    public enum OctopusRetreatReason
    {
        Defeated = 0,
        PlayerInked = 1,
        LevelEnded = 2
    }

    /// <summary>
    /// Level 4 periodic enemy built on ObligationBase with growing DPS, pooled ink balls, radar warning/combat states, and HuntingWeapon hits.
    /// </summary>
    public sealed class OctopusEnemy : ObligationBase, ICatchable, IRadarTrackable
    {
        private enum OctopusPhase
        {
            Hidden = 0,
            Warning = 1,
            Combat = 2,
            Laughing = 3
        }

        [Header("Configuration & References")]
        [SerializeField] private OctopusConfig _octopusConfig;
        [SerializeField] private TrackableRegistry _trackableRegistry;
        [SerializeField] private ScreenInkController _screenInk;
        [SerializeField] private Transform _playerTarget;
        [SerializeField] private Transform _spawnPointOverride;

        [Header("Ink Ball Pool")]
        [SerializeField] private InkBall _inkBallPrefab;
        [SerializeField] private Transform _inkPoolContainer;

        private readonly Stack<InkBall> _inkPool = new Stack<InkBall>();
        private readonly List<InkBall> _activeInkBalls = new List<InkBall>();

        private OctopusPhase _phase = OctopusPhase.Hidden;
        private Vector3 _anchorPosition;
        private int _maxHits = 3;
        private int _currentHitsRemaining;
        private float _inkSpawnInterval = 4f;
        private float _inkBallSpeed = 8f;
        private float _laughDelay = 1.5f;
        private float _warningTimer;
        private float _inkTimer;
        private float _laughTimer;
        private float _invulnerableTimer;
        private float _moveElapsed;
        private bool _poolInitialized;

        /// <summary>
        /// Raised when the octopus appears and enters its warning state.
        /// </summary>
        public event Action OctopusAppeared;

        /// <summary>
        /// Raised when the octopus takes a HuntingWeapon hit, passing (remainingHits, maxHits).
        /// </summary>
        public event Action<int, int> OctopusHit;

        /// <summary>
        /// Raised when a HuntingWeapon shot hits the octopus while invulnerable after throwing ink.
        /// </summary>
        public event Action OctopusHitBlocked;

        /// <summary>
        /// Raised when the octopus retreats, passing the retreat reason.
        /// </summary>
        public event Action<OctopusRetreatReason> OctopusRetreated;

        /// <summary>
        /// Raised when screen ink crosses the retreat threshold and the octopus laughs before retreating.
        /// </summary>
        public event Action OctopusLaughed;

        /// <summary>
        /// Raised when an ink ball is spawned from the pool.
        /// </summary>
        public event Action<InkBall> InkBallSpawned;

        /// <summary>
        /// Raised when an ink ball is shot down by HuntingWeapon.
        /// </summary>
        public event Action<InkBall> InkBallIntercepted;

        /// <summary>
        /// Raised when an ink ball reaches the player target and splashes screen ink.
        /// </summary>
        public event Action<InkBall> InkSplashed;

        public event Action<IRadarTrackable> TrackableStateChanged;

        public bool IsPresent => _phase != OctopusPhase.Hidden;
        public bool IsInvulnerable => _invulnerableTimer > 0f;
        public int CurrentHitsRemaining => _currentHitsRemaining;
        public int MaxHits => _maxHits;

        public bool CanCatch => (_phase == OctopusPhase.Warning || _phase == OctopusPhase.Combat) && _currentHitsRemaining > 0;

        public RadarEntityType EntityType => RadarEntityType.Enemy;
        public RadarSignalColor RadarColor => _phase == OctopusPhase.Warning ? RadarSignalColor.Orange : RadarSignalColor.Red;
        public float RadarSeverity => IsPresent ? 1f : 0f;
        public Vector3 WorldPosition => transform.position;
        public bool IsTrackableActive => IsPresent;

        protected override bool RespectsGlobalDamagePause => false;

        private void Awake()
        {
            EnsureInkPoolInitialized();
        }

        private void OnEnable()
        {
            if (_trackableRegistry != null)
            {
                _trackableRegistry.Register(this);
            }
        }

        private void OnDisable()
        {
            if (_trackableRegistry != null)
            {
                _trackableRegistry.Unregister(this);
            }
        }

        protected override void Update()
        {
            if (_phase == OctopusPhase.Hidden)
            {
                return;
            }

            if (_phase == OctopusPhase.Laughing)
            {
                _laughTimer -= Time.deltaTime;
                if (_laughTimer <= 0f)
                {
                    Retreat(OctopusRetreatReason.PlayerInked);
                }

                return;
            }

            UpdateZoneMovement();

            if (_phase == OctopusPhase.Warning)
            {
                _warningTimer -= Time.deltaTime;
                if (_warningTimer <= 0f)
                {
                    EnterCombatState();
                }

                return;
            }

            base.Update();

            if (_invulnerableTimer > 0f)
            {
                _invulnerableTimer = Mathf.Max(0f, _invulnerableTimer - Time.deltaTime);
            }

            _inkTimer -= Time.deltaTime;
            if (_inkTimer <= 0f)
            {
                _inkTimer = _inkSpawnInterval;
                TrySpawnInkBall();
            }
        }

        /// <summary>
        /// Injects runtime encounter settings from LevelConfig before appearing.
        /// </summary>
        public void Configure(
            ReefHealth reefHealth,
            int maxHits,
            float inkSpawnInterval,
            float inkBallSpeed,
            float laughDelay)
        {
            ObligationConfig obligationConfig = Config != null
                ? Config
                : (_octopusConfig != null ? _octopusConfig.Obligation : null);

            SetDependencies(reefHealth, obligationConfig);
            _maxHits = Mathf.Max(1, maxHits);
            _inkSpawnInterval = Mathf.Max(0.1f, inkSpawnInterval);
            _inkBallSpeed = Mathf.Max(0.1f, inkBallSpeed);
            _laughDelay = Mathf.Max(0f, laughDelay);
        }

        /// <summary>
        /// Spawns the octopus at its fixed world position behind the player in warning state.
        /// </summary>
        public void Appear()
        {
            if (IsPresent || _octopusConfig == null)
            {
                return;
            }

            EnsureInkPoolInitialized();

            _anchorPosition = _spawnPointOverride != null
                ? _spawnPointOverride.position
                : _octopusConfig.SpawnPosition;
            transform.position = _anchorPosition;

            _currentHitsRemaining = _maxHits;
            _moveElapsed = 0f;
            _warningTimer = _octopusConfig.WarningDuration;
            _inkTimer = _inkSpawnInterval;
            _laughTimer = 0f;
            _invulnerableTimer = 0f;
            _phase = OctopusPhase.Warning;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            else if (_trackableRegistry != null)
            {
                _trackableRegistry.Register(this);
            }

            TrackableStateChanged?.Invoke(this);
            OctopusAppeared?.Invoke();

            if (_warningTimer <= 0f)
            {
                EnterCombatState();
            }
        }

        /// <summary>
        /// Registers a HuntingWeapon hit against the octopus and retreats when hits reach zero.
        /// </summary>
        public bool TryCatch()
        {
            if (!CanCatch)
            {
                return false;
            }

            if (IsInvulnerable)
            {
                OctopusHitBlocked?.Invoke();
                return false;
            }

            if (_phase == OctopusPhase.Warning)
            {
                EnterCombatState();
            }

            _currentHitsRemaining = Mathf.Max(0, _currentHitsRemaining - 1);
            OctopusHit?.Invoke(_currentHitsRemaining, _maxHits);

            if (_currentHitsRemaining <= 0)
            {
                Retreat(OctopusRetreatReason.Defeated);
            }

            return true;
        }

        /// <summary>
        /// Ends the active encounter with a reason and hides the octopus for future appearances.
        /// </summary>
        public void Retreat(OctopusRetreatReason reason)
        {
            if (!IsPresent)
            {
                return;
            }

            ReturnAllActiveInkBallsToPool();
            ResetObligationState();
            _invulnerableTimer = 0f;
            _phase = OctopusPhase.Hidden;
            TrackableStateChanged?.Invoke(this);

            if (_trackableRegistry != null)
            {
                _trackableRegistry.Unregister(this);
            }

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            OctopusRetreated?.Invoke(reason);
        }

        /// <summary>
        /// Resets octopus and pooled ink ball state without destroying any objects.
        /// </summary>
        public void ResetState()
        {
            ReturnAllActiveInkBallsToPool();
            ResetObligationState();
            _phase = OctopusPhase.Hidden;
            _currentHitsRemaining = 0;
            _warningTimer = 0f;
            _inkTimer = 0f;
            _laughTimer = 0f;
            _invulnerableTimer = 0f;
            _moveElapsed = 0f;
            TrackableStateChanged?.Invoke(this);

            if (_trackableRegistry != null)
            {
                _trackableRegistry.Unregister(this);
            }

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void EnterCombatState()
        {
            if (_phase != OctopusPhase.Warning)
            {
                return;
            }

            _phase = OctopusPhase.Combat;
            BeginPending();
            TrackableStateChanged?.Invoke(this);
        }

        private void UpdateZoneMovement()
        {
            if (_octopusConfig == null)
            {
                return;
            }

            float speed = _octopusConfig.ZoneMoveSpeed;
            Vector2 amplitude = _octopusConfig.ZoneMoveAmplitude;
            if (speed <= 0f || (amplitude.x <= 0f && amplitude.y <= 0f))
            {
                return;
            }

            _moveElapsed += Time.deltaTime;
            float angle = _moveElapsed * speed;
            Vector3 offset = new Vector3(
                Mathf.Sin(angle) * amplitude.x,
                Mathf.Cos(angle) * amplitude.y,
                0f);
            transform.position = _anchorPosition + offset;
        }

        private void EnsureInkPoolInitialized()
        {
            if (_poolInitialized || _inkBallPrefab == null || _octopusConfig == null)
            {
                return;
            }

            _poolInitialized = true;
            Transform parent = _inkPoolContainer != null ? _inkPoolContainer : transform.parent;
            int poolSize = _octopusConfig.InkPoolSize;

            for (int i = 0; i < poolSize; i++)
            {
                InkBall instance = Instantiate(_inkBallPrefab, parent);
                instance.ResetState();
                _inkPool.Push(instance);
            }
        }

        private void TrySpawnInkBall()
        {
            if (_octopusConfig == null || _playerTarget == null || _inkPool.Count == 0)
            {
                return;
            }

            InkBall ball = null;
            while (_inkPool.Count > 0 && ball == null)
            {
                ball = _inkPool.Pop();
            }

            if (ball == null)
            {
                return;
            }

            _activeInkBalls.Add(ball);
            Vector3 spawnPosition = transform.position + _octopusConfig.InkSpawnOffset;
            ball.Launch(
                spawnPosition,
                _playerTarget,
                _inkBallSpeed,
                _octopusConfig.InkHitDistance,
                _octopusConfig.InkBallMaxLifetime,
                HandleInkBallReachedPlayer,
                HandleInkBallIntercepted);

            _invulnerableTimer = _octopusConfig.InvulnerableDuration;
            InkBallSpawned?.Invoke(ball);
        }

        private void HandleInkBallReachedPlayer(InkBall ball)
        {
            ReturnInkBallToPool(ball);
            InkSplashed?.Invoke(ball);

            if (_octopusConfig == null || _screenInk == null)
            {
                return;
            }

            _screenInk.AddInk(_octopusConfig.InkAmountPerBall);

            if (_phase == OctopusPhase.Laughing || !IsPresent)
            {
                return;
            }

            if (_screenInk.InkLevel >= _octopusConfig.RetreatInkThreshold)
            {
                EnterLaughingState();
            }
        }

        private void HandleInkBallIntercepted(InkBall ball)
        {
            ReturnInkBallToPool(ball);
            InkBallIntercepted?.Invoke(ball);
        }

        private void EnterLaughingState()
        {
            Resolve();
            _phase = OctopusPhase.Laughing;
            _laughTimer = _laughDelay;
            OctopusLaughed?.Invoke();

            if (_laughTimer <= 0f)
            {
                Retreat(OctopusRetreatReason.PlayerInked);
            }
        }

        private void ReturnInkBallToPool(InkBall ball)
        {
            if (ball == null)
            {
                return;
            }

            _activeInkBalls.Remove(ball);
            if (!_inkPool.Contains(ball))
            {
                _inkPool.Push(ball);
            }
        }

        private void ReturnAllActiveInkBallsToPool()
        {
            for (int i = _activeInkBalls.Count - 1; i >= 0; i--)
            {
                InkBall ball = _activeInkBalls[i];
                if (ball == null)
                {
                    continue;
                }

                ball.ResetState();
                if (!_inkPool.Contains(ball))
                {
                    _inkPool.Push(ball);
                }
            }

            _activeInkBalls.Clear();
        }
    }
}
