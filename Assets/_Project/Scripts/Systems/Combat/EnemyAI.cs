using UnityEngine;
using UnityEngine.AI;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Player;

namespace CultivationGame.Systems
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour, IDamageable
    {
        [Header("Configuration")]
        public EnemyData enemyData;

        [Header("References")]
        public HealthSystem healthSystem;

        public EnemyState CurrentState { get; private set; } = EnemyState.Idle;
        public bool IsDead => healthSystem != null && healthSystem.IsDead;

        private Transform _target;
        private IDamageable _targetDamageable;
        private Vector3 _spawnPoint;
        private Vector3 _patrolTarget;
        private float _attackTimer;
        private float _idleTimer;
        private float _stateTimer;
        private NavMeshAgent _agent;

        private const float IdleDurationMin = 1f;
        private const float IdleDurationMax = 3f;
        private const float AttackRangeHysteresis = 1.2f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            _spawnPoint = transform.position;

            if (_agent != null && enemyData != null)
            {
                _agent.speed = enemyData.moveSpeed;
                _agent.updateRotation = false; // We handle rotation with smooth slerp
            }

            if (healthSystem != null && enemyData != null)
            {
                healthSystem.Initialize(enemyData.maxHealth);
                healthSystem.OnDied += HandleDeath;
            }

            SetState(EnemyState.Idle);
            GameDataEvents.RaiseEnemySpawned(this);
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
                healthSystem.OnDied -= HandleDeath;
        }

        private void Update()
        {
            if (IsDead) return;
            if (enemyData == null) return;

            FindTarget();
            _attackTimer -= Time.deltaTime;
            _stateTimer -= Time.deltaTime;

            switch (CurrentState)
            {
                case EnemyState.Idle:
                    UpdateIdle();
                    break;
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;
                case EnemyState.Chase:
                    UpdateChase();
                    break;
                case EnemyState.Attack:
                    UpdateAttack();
                    break;
                case EnemyState.Return:
                    UpdateReturn();
                    break;
            }
        }

        // --- State Machine ---

        private void SetState(EnemyState newState)
        {
            CurrentState = newState;
            _stateTimer = 0f;

            switch (newState)
            {
                case EnemyState.Idle:
                    _idleTimer = Random.Range(IdleDurationMin, IdleDurationMax);
                    StopAgent();
                    break;
                case EnemyState.Patrol:
                    PickPatrolTarget();
                    NavigateTo(_patrolTarget);
                    break;
                case EnemyState.Chase:
                    // Destination is updated each frame in UpdateChase
                    break;
                case EnemyState.Attack:
                    StopAgent();
                    break;
                case EnemyState.Return:
                    NavigateTo(_spawnPoint);
                    break;
            }
        }

        // --- Idle ---

        private void UpdateIdle()
        {
            _idleTimer -= Time.deltaTime;

            if (CanDetectTarget())
            {
                SetState(EnemyState.Chase);
                return;
            }

            if (_idleTimer <= 0f)
            {
                SetState(EnemyState.Patrol);
            }
        }

        // --- Patrol ---

        private void UpdatePatrol()
        {
            if (CanDetectTarget())
            {
                SetState(EnemyState.Chase);
                return;
            }

            if (HasArrivedAtDestination())
            {
                SetState(EnemyState.Idle);
            }

            RotateAlongPath();
        }

        private void PickPatrolTarget()
        {
            Vector2 randomOffset = Random.insideUnitCircle * enemyData.patrolRadius;
            Vector3 candidate = _spawnPoint + new Vector3(randomOffset.x, 0f, randomOffset.y);

            // Snap patrol target to a valid NavMesh position
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, enemyData.patrolRadius, NavMesh.AllAreas))
            {
                _patrolTarget = hit.position;
            }
            else
            {
                _patrolTarget = _spawnPoint;
            }
        }

        // --- Chase ---

        private void UpdateChase()
        {
            if (_target == null || !CanDetectTarget())
            {
                // Lost target — check if too far from spawn
                float sqrDistFromSpawn = (transform.position - _spawnPoint).sqrMagnitude;
                if (sqrDistFromSpawn > enemyData.leashRange * enemyData.leashRange)
                {
                    SetState(EnemyState.Return);
                }
                else
                {
                    SetState(EnemyState.Idle);
                }
                return;
            }

            float sqrDistToTarget = (transform.position - _target.position).sqrMagnitude;

            // Leash check
            float sqrDistFromHome = (transform.position - _spawnPoint).sqrMagnitude;
            if (sqrDistFromHome > enemyData.leashRange * enemyData.leashRange)
            {
                SetState(EnemyState.Return);
                return;
            }

            if (sqrDistToTarget <= enemyData.attackRange * enemyData.attackRange)
            {
                SetState(EnemyState.Attack);
                return;
            }

            // Update destination each frame since the player is moving
            NavigateTo(_target.position);
            RotateAlongPath();
        }

        // --- Attack ---

        private void UpdateAttack()
        {
            if (_target == null || (_targetDamageable != null && _targetDamageable.IsDead))
            {
                SetState(EnemyState.Idle);
                return;
            }

            float sqrDistToTarget = (transform.position - _target.position).sqrMagnitude;
            float extendedRange = enemyData.attackRange * AttackRangeHysteresis;

            // If target moved out of attack range, chase again
            if (sqrDistToTarget > extendedRange * extendedRange)
            {
                SetState(EnemyState.Chase);
                return;
            }

            // Face the target
            LookAt(_target.position);

            // Attack on cooldown
            if (_attackTimer <= 0f)
            {
                PerformAttack();
                _attackTimer = enemyData.attackCooldown;
            }
        }

        private void PerformAttack()
        {
            if (_target == null) return;

            IDamageable damageable = _target.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(enemyData.damage, gameObject);
            }
        }

        // --- Return ---

        private void UpdateReturn()
        {
            if (HasArrivedAtDestination())
            {
                SetState(EnemyState.Idle);
                return;
            }

            RotateAlongPath();
        }

        // --- IDamageable Implementation ---

        public void TakeDamage(float damage, GameObject attacker)
        {
            if (IsDead) return;

            // Realm gate: attackers below the required cultivation realm cannot
            // harm this enemy (they still draw aggro).
            if (enemyData != null && enemyData.minimumRealm != null && attacker != null)
            {
                var attackerStats = attacker.GetComponent<PlayerStats>();
                if (attackerStats != null &&
                    (attackerStats.currentRealm == null ||
                     attackerStats.currentRealm.realmIndex < enemyData.minimumRealm.realmIndex))
                {
                    damage = 0f;
                }
            }

            if (healthSystem != null && damage > 0f)
            {
                healthSystem.TakeDamage(damage);
                GameDataEvents.RaiseEnemyDamaged(this, damage);
            }

            // Aggro: switch target to attacker
            if (attacker != null && CurrentState != EnemyState.Dead)
            {
                _target = attacker.transform;
                if (CurrentState != EnemyState.Attack)
                {
                    SetState(EnemyState.Chase);
                }
            }
        }

        // --- Death ---

        private void HandleDeath()
        {
            CurrentState = EnemyState.Dead;
            StopAgent();
            GameDataEvents.RaiseEnemyDied(this);

            // Disable collider so player can walk through
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Disable agent so it doesn't interfere with death effects
            if (_agent != null) _agent.enabled = false;

            // Destroy after short delay to allow death effects
            Destroy(gameObject, 2f);
        }

        // --- Helpers ---

        private void FindTarget()
        {
            if (_target != null) return;

            // Only search once; the reference is cached for the lifetime of this enemy.
            _target = FindPlayerTransform();
            _targetDamageable = _target != null ? _target.GetComponent<IDamageable>() : null;
        }

        private static Transform _cachedPlayer;

        private static Transform FindPlayerTransform()
        {
            if (_cachedPlayer != null) return _cachedPlayer;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _cachedPlayer = player.transform;

            return _cachedPlayer;
        }

        private bool CanDetectTarget()
        {
            if (_target == null) return false;
            // A dead player is no longer a target — enemies disengage instead of
            // standing at the corpse attacking forever.
            if (_targetDamageable != null && _targetDamageable.IsDead) return false;
            float sqrDist = (transform.position - _target.position).sqrMagnitude;
            return sqrDist <= enemyData.detectionRange * enemyData.detectionRange;
        }

        // --- NavMeshAgent Helpers ---

        private void NavigateTo(Vector3 destination)
        {
            if (_agent == null || !_agent.isOnNavMesh) return;
            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }

        private void StopAgent()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        private bool HasArrivedAtDestination()
        {
            if (_agent == null) return false;
            return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance;
        }

        /// <summary>
        /// Smoothly rotates the enemy along the NavMeshAgent's current movement direction.
        /// </summary>
        private void RotateAlongPath()
        {
            if (_agent != null && _agent.velocity.sqrMagnitude > 0.01f)
            {
                LookAt(transform.position + _agent.velocity);
            }
        }

        private void LookAt(Vector3 target)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }
    }
}
