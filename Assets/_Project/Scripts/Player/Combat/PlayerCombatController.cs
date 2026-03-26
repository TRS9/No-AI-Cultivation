using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Player
{
    public class PlayerCombatController : MonoBehaviour, IDamageable
    {
        [Header("References")]
        public PlayerStats playerStats;
        public HealthSystem healthSystem;

        [Header("Attack")]
        [Tooltip("Radius of the attack hit detection sphere")]
        public float attackRadius = 2f;
        [Tooltip("Offset in front of the player for the attack sphere center")]
        public float attackForwardOffset = 1f;
        [Tooltip("Seconds between attacks")]
        public float attackCooldown = 0.5f;
        public LayerMask enemyLayer;

        [Header("Dodge Roll")]
        [Tooltip("Distance covered during a dodge roll")]
        public float dodgeDistance = 5f;
        [Tooltip("Duration of the dodge roll in seconds")]
        public float dodgeDuration = 0.3f;
        [Tooltip("Cooldown between dodge rolls in seconds")]
        public float dodgeCooldown = 1f;
        [Tooltip("Stamina cost per dodge roll")]
        public float dodgeStaminaCost = 25f;

        [Header("Input References")]
        public InputActionReference attackAction;
        public InputActionReference dodgeAction;

        [Header("Animation")]
        public Animator animator;

        public bool IsDead => healthSystem != null && healthSystem.IsDead;
        public bool IsDodging { get; private set; }
        public bool IsAttacking { get; private set; }

        private float _attackTimer;
        private float _dodgeTimer;
        private Rigidbody _rb;
        private Vector3 _dodgeDirection;
        private float _dodgeElapsed;
        private PlayerMovement _playerMovement;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _playerMovement = GetComponent<PlayerMovement>();
        }

        private void Start()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += HandleHealthChanged;
                healthSystem.OnDied += HandlePlayerDied;

                // Broadcast initial health state
                GameEvents.RaisePlayerHealthChanged(healthSystem.CurrentHealth, healthSystem.MaxHealth);
            }
        }

        private void OnEnable()
        {
            if (attackAction != null)
                attackAction.action.performed += HandleAttackInput;
            if (dodgeAction != null)
                dodgeAction.action.performed += HandleDodgeInput;
        }

        private void OnDisable()
        {
            if (attackAction != null)
                attackAction.action.performed -= HandleAttackInput;
            if (dodgeAction != null)
                dodgeAction.action.performed -= HandleDodgeInput;
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= HandleHealthChanged;
                healthSystem.OnDied -= HandlePlayerDied;
            }
        }

        private void Update()
        {
            if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;
            if (_dodgeTimer > 0f) _dodgeTimer -= Time.deltaTime;

            if (IsDodging) UpdateDodge();
        }

        // --- Input Handlers ---

        private void HandleAttackInput(InputAction.CallbackContext context)
        {
            if (IsDead || IsDodging || IsAttacking) return;
            if (_attackTimer > 0f) return;

            PerformAttack();
        }

        private void HandleDodgeInput(InputAction.CallbackContext context)
        {
            if (IsDead || IsDodging) return;
            if (_dodgeTimer > 0f) return;

            // Check stamina
            if (_playerMovement != null && _playerMovement.currentStamina < dodgeStaminaCost) return;

            PerformDodge();
        }

        // --- Attack ---

        private void PerformAttack()
        {
            IsAttacking = true;
            _attackTimer = attackCooldown;

            float damage = CalculatePlayerDamage();

            // Detect enemies in front of the player
            Vector3 attackCenter = transform.position + transform.forward * attackForwardOffset;
            Collider[] hits = Physics.OverlapSphere(attackCenter, attackRadius, enemyLayer);

            foreach (Collider hit in hits)
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target != null && !target.IsDead)
                {
                    target.TakeDamage(damage, gameObject);
                }
            }

            GameEvents.RaisePlayerAttack();

            if (animator != null)
                animator.SetTrigger("Attack");

            // Reset attack state after a short delay
            StartCoroutine(ResetAttackAfterDelay());
        }

        private IEnumerator ResetAttackAfterDelay()
        {
            yield return new WaitForSeconds(0.2f);
            IsAttacking = false;
        }

        private float CalculatePlayerDamage()
        {
            float baseDamage = 5f;
            if (playerStats != null && playerStats.currentRealm != null)
            {
                baseDamage = playerStats.currentRealm.baseDamage;
            }

            return baseDamage * CultivationBuffs.DamageMultiplier;
        }

        // --- Dodge Roll ---

        private void PerformDodge()
        {
            IsDodging = true;
            _dodgeTimer = dodgeCooldown;
            _dodgeElapsed = 0f;

            // Drain stamina
            if (_playerMovement != null)
            {
                _playerMovement.currentStamina -= dodgeStaminaCost;
                GameEvents.RaiseStaminaChanged(_playerMovement.currentStamina, _playerMovement.maxStamina);
            }

            // Dodge in movement direction, or forward if standing still
            Vector3 horizontalVelocity = _rb != null
                ? new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z)
                : Vector3.zero;

            if (horizontalVelocity.sqrMagnitude > 0.1f)
            {
                _dodgeDirection = horizontalVelocity.normalized;
            }
            else
            {
                _dodgeDirection = transform.forward;
            }

            GameEvents.RaisePlayerDodge();

            if (animator != null)
                animator.SetTrigger("Dodge");
        }

        private void UpdateDodge()
        {
            _dodgeElapsed += Time.deltaTime;
            if (_dodgeElapsed >= dodgeDuration)
            {
                IsDodging = false;
                return;
            }

            // Move player during dodge
            float speed = dodgeDistance / dodgeDuration;
            if (_rb != null)
            {
                _rb.linearVelocity = new Vector3(
                    _dodgeDirection.x * speed,
                    _rb.linearVelocity.y,
                    _dodgeDirection.z * speed
                );
            }
        }

        // --- IDamageable Implementation ---

        public void TakeDamage(float damage, GameObject attacker)
        {
            if (IsDead || IsDodging) return; // I-frames during dodge

            float defense = 0f;
            if (playerStats != null && playerStats.currentRealm != null)
            {
                defense = playerStats.currentRealm.baseDefense;
            }

            // Apply defense reduction with buff multiplier
            float effectiveDefense = defense * CultivationBuffs.DefenseMultiplier;
            float finalDamage = Mathf.Max(damage - effectiveDefense, 1f);

            if (healthSystem != null)
            {
                healthSystem.TakeDamage(finalDamage);
            }
        }

        // --- Event Handlers ---

        private void HandleHealthChanged(float current, float max)
        {
            GameEvents.RaisePlayerHealthChanged(current, max);
        }

        private void HandlePlayerDied()
        {
            GameEvents.RaisePlayerDied();
            Debug.Log("Player has been defeated!");
        }
    }
}
