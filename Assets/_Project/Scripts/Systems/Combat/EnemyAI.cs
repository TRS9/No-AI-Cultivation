using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    public class EnemyAI : MonoBehaviour, IDamageable
    {
        [Header("Configuration")]
        public EnemyData enemyData;

        [Header("References")]
        public HealthSystem healthSystem;

        public EnemyState CurrentState { get; private set; } = EnemyState.Idle;
        public bool IsDead => healthSystem != null && healthSystem.IsDead;

        private Transform _target;
        private Vector3 _spawnPoint;
        private Vector3 _patrolTarget;
        private float _attackTimer;
        private float _idleTimer;
        private float _stateTimer;

        private const float IdleDurationMin = 1f;
        private const float IdleDurationMax = 3f;
        private const float PatrolArrivalThreshold = 0.5f;

        private void Start()
        {
            _spawnPoint = transform.position;

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
                    break;
                case EnemyState.Patrol:
                    PickPatrolTarget();
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

            MoveToward(_patrolTarget);

            float distToTarget = Vector3.Distance(transform.position, _patrolTarget);
            if (distToTarget < PatrolArrivalThreshold)
            {
                SetState(EnemyState.Idle);
            }
        }

        private void PickPatrolTarget()
        {
            Vector2 randomOffset = Random.insideUnitCircle * enemyData.patrolRadius;
            _patrolTarget = _spawnPoint + new Vector3(randomOffset.x, 0f, randomOffset.y);
        }

        // --- Chase ---

        private void UpdateChase()
        {
            if (_target == null || !CanDetectTarget())
            {
                // Lost target — check if too far from spawn
                float distFromSpawn = Vector3.Distance(transform.position, _spawnPoint);
                if (distFromSpawn > enemyData.leashRange)
                {
                    SetState(EnemyState.Return);
                }
                else
                {
                    SetState(EnemyState.Idle);
                }
                return;
            }

            float distToTarget = Vector3.Distance(transform.position, _target.position);

            // Leash check
            float distFromHome = Vector3.Distance(transform.position, _spawnPoint);
            if (distFromHome > enemyData.leashRange)
            {
                SetState(EnemyState.Return);
                return;
            }

            if (distToTarget <= enemyData.attackRange)
            {
                SetState(EnemyState.Attack);
                return;
            }

            MoveToward(_target.position);
        }

        // --- Attack ---

        private void UpdateAttack()
        {
            if (_target == null)
            {
                SetState(EnemyState.Idle);
                return;
            }

            float distToTarget = Vector3.Distance(transform.position, _target.position);

            // If target moved out of attack range, chase again
            if (distToTarget > enemyData.attackRange * 1.2f)
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
            MoveToward(_spawnPoint);

            float distToSpawn = Vector3.Distance(transform.position, _spawnPoint);
            if (distToSpawn < PatrolArrivalThreshold)
            {
                SetState(EnemyState.Idle);
            }
        }

        // --- IDamageable Implementation ---

        public void TakeDamage(float damage, GameObject attacker)
        {
            if (IsDead) return;

            if (healthSystem != null)
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
            GameDataEvents.RaiseEnemyDied(this);

            // Disable collider so player can walk through
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Destroy after short delay to allow death effects
            Destroy(gameObject, 2f);
        }

        // --- Helpers ---

        private void FindTarget()
        {
            if (_target != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
            }
        }

        private bool CanDetectTarget()
        {
            if (_target == null) return false;
            float dist = Vector3.Distance(transform.position, _target.position);
            return dist <= enemyData.detectionRange;
        }

        private void MoveToward(Vector3 destination)
        {
            Vector3 direction = (destination - transform.position);
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f) return;

            direction.Normalize();
            transform.position += direction * enemyData.moveSpeed * Time.deltaTime;

            LookAt(destination);
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
