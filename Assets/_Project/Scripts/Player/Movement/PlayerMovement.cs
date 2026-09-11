using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;

namespace CultivationGame.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Physics & Movement")]
        public Rigidbody rb;
        public float moveSpeed = 1.8f;
        public float sprintSpeed = 4.8f;
        public float acceleration = 22f;
        public float braking = 45f;
        public float airAcceleration = 7f;
        public float fallGravityMultiplier = 2f;
        public float jumpForce = 6f;
        public float rotationSpeed = 600f;
        public LayerMask groundLayer;
        public float maxDistanceRay = 1.1f;
        [Tooltip("Radius of the sphere used for ground detection — handles slopes and uneven terrain")]
        public float groundCheckRadius = 0.3f;

        [Header("Stamina System")]
        public float maxStamina = 100f;
        public float staminaDrainRate = 20f;
        public float staminaRegenRate = 15f;
        public float staminaRegenDelay = 1.0f;
        public float currentStamina;

        private float _currentSpeed;
        private float _regenTimer;
        private float _lastRaisedStamina = float.MinValue;
        private Vector2 _moveDirection;
        private Vector3 _targetMoveVector;
        private Vector3 _smoothedDirection;
        private CapsuleCollider _capsule;
        private PlayerCombatController _combat;
        private Vector3 _groundNormal = Vector3.up;
        private float _jumpBufferedUntil = float.NegativeInfinity;
        private bool _jumpConsumed;
        private bool _dead;
        private bool _meditating;
        public bool IsControlBlocked => isControlBlocked;

        // Cached per-physics-frame ground state — used by Update and input callbacks
        // so IsGrounded() isn't re-evaluated on different threads/timings.
        private bool _isGrounded;

        // Coyote time: allow jumping within this window after walking off a ledge
        private float _lastGroundedTime = float.NegativeInfinity;
        private const float CoyoteTime = 0.15f;

        private Camera _camera;

        [Header("Animation")]
        public Animator animator;

        [Header("Input References")]
        public InputActionReference move;
        public InputActionReference jump;
        public InputActionReference sprint;

        private bool isControlBlocked;

        private void Start()
        {
            _capsule = GetComponent<CapsuleCollider>();
            _combat = GetComponent<PlayerCombatController>();
            currentStamina = maxStamina;
            GameEvents.RaiseStaminaChanged(currentStamina, maxStamina);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            if (jump != null) jump.action.performed += HandleJump;
            GameEvents.OnMeditationToggled += HandleMeditationBlock;
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnPlayerRespawned += HandlePlayerRespawned;
        }

        private void OnDisable()
        {
            if (jump != null) jump.action.performed -= HandleJump;
            GameEvents.OnMeditationToggled -= HandleMeditationBlock;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnPlayerRespawned -= HandlePlayerRespawned;
        }

        private void Update()
        {
            if (!isControlBlocked) ReadInput();
            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            _isGrounded = ProbeGround();
            if (_isGrounded)
            {
                _lastGroundedTime = Time.time;
                _jumpConsumed = false;
            }
            if (!isControlBlocked && !_jumpConsumed && Time.time <= _jumpBufferedUntil &&
                (_isGrounded || Time.time - _lastGroundedTime <= CoyoteTime))
            {
                _jumpConsumed = true;
                _isGrounded = false;
                _jumpBufferedUntil = float.NegativeInfinity;
                _lastGroundedTime = float.NegativeInfinity;
                Vector3 velocity = rb.linearVelocity;
                velocity.y = 0;
                rb.linearVelocity = velocity;
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                animator?.SetTrigger("Jump");
            }
            if (_combat != null && _combat.IsDodging) return;
            ComputeMoveVector();
            HandleStaminaAndSpeed(_isGrounded);
            ApplyMovement();
            ApplyRotation();
            if (!_isGrounded && rb.linearVelocity.y < 0)
                rb.AddForce(Physics.gravity * (Mathf.Max(1, fallGravityMultiplier) - 1), ForceMode.Acceleration);
        }

        private void ReadInput()
        {
            _moveDirection = move != null ? Vector2.ClampMagnitude(move.action.ReadValue<Vector2>(), 1) : Vector2.zero;
        }

        // Runs in FixedUpdate so direction is computed once per physics tick,
        // preventing camera-feedback oscillation that occurs at variable Update rate.
        private void ComputeMoveVector()
        {
            if (_moveDirection.sqrMagnitude > 0.01f)
            {
                if (_camera == null) return;

                Vector3 forward = _camera.transform.forward;
                Vector3 right   = _camera.transform.right;

                forward.y = 0;
                right.y   = 0;
                forward.Normalize();
                right.Normalize();

                Vector3 desired = (forward * _moveDirection.y + right * _moveDirection.x).normalized;

                // Slerp to absorb camera-follow micro-oscillations that cause rotation jitter
                _smoothedDirection = (_smoothedDirection.sqrMagnitude > 0.01f)
                    ? Vector3.Slerp(_smoothedDirection, desired, 15f * Time.fixedDeltaTime)
                    : desired;
                _targetMoveVector = _smoothedDirection;
            }
            else
            {
                _targetMoveVector = Vector3.zero;
                _smoothedDirection = Vector3.zero;
            }
        }

        private void ApplyRotation()
        {
            if (_targetMoveVector.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(_targetMoveVector);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime));
            }
        }

        private void HandleStaminaAndSpeed(bool isGrounded)
        {
            bool isMoving    = _moveDirection.sqrMagnitude > 0.01f;
            bool isSprinting = sprint != null && sprint.action.IsPressed() && currentStamina > 0 && isMoving && isGrounded;

            if (isSprinting)
            {
                _currentSpeed = sprintSpeed;
                currentStamina -= staminaDrainRate * Time.fixedDeltaTime;
                _regenTimer    = staminaRegenDelay;
            }
            else
            {
                if (isGrounded) _currentSpeed = moveSpeed;
                else _currentSpeed = Mathf.Max(_currentSpeed, moveSpeed);

                if (_regenTimer > 0)
                    _regenTimer -= Time.fixedDeltaTime;
                else
                    currentStamina += staminaRegenRate * Time.fixedDeltaTime;
            }

            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

            // Only notify the UI when the value actually moved — this runs every
            // physics tick and used to rebuild the stamina bar 50×/s while idle.
            if (!Mathf.Approximately(currentStamina, _lastRaisedStamina))
            {
                _lastRaisedStamina = currentStamina;
                GameEvents.RaiseStaminaChanged(currentStamina, maxStamina);
            }
        }

        private void ApplyMovement()
        {
            Vector3 velocity = rb.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0, velocity.z);
            Vector3 desired = _targetMoveVector * _currentSpeed * _moveDirection.magnitude;
            float rate = _isGrounded ? (_moveDirection.sqrMagnitude > .01f ? acceleration : braking) : airAcceleration;
            horizontal = Vector3.MoveTowards(horizontal, desired, rate * Time.fixedDeltaTime);
            if (isControlBlocked) horizontal = Vector3.zero;
            float vertical = velocity.y;
            if (_isGrounded && !_jumpConsumed)
            {
                Vector3 tangent = Vector3.ProjectOnPlane(horizontal, _groundNormal);
                horizontal.x = tangent.x; horizontal.z = tangent.z;
                vertical = tangent.y - .5f;
            }
            rb.linearVelocity = new Vector3(horizontal.x, vertical, horizontal.z);
        }

        private void HandleJump(InputAction.CallbackContext context)
        {
            if (isControlBlocked || (_combat != null && _combat.IsDodging)) return;
            _jumpBufferedUntil = Time.time + .12f;
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            animator.SetFloat("Speed", horizontalVelocity.magnitude);
            animator.SetBool("IsGrounded", _isGrounded); // cached value — not a fresh raycast
        }

        public bool IsGrounded() => _isGrounded;

        private bool ProbeGround()
        {
            // Uphill travel has positive vertical velocity too. Only suppress
            // the probe during a rising jump, not while following a slope.
            if (_capsule == null || (rb.linearVelocity.y > .1f && (!_isGrounded || _jumpConsumed))) return false;
            Bounds bounds = _capsule.bounds;
            float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * .9f;
            Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius + .05f, bounds.center.z);
            if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, .11f,
                groundLayer, QueryTriggerInteraction.Ignore) && Vector3.Dot(hit.normal, Vector3.up) >= .65f)
            {
                _groundNormal = hit.normal;
                return true;
            }
            _groundNormal = Vector3.up;
            return false;
        }

        private void HandleMeditationBlock(bool meditating)
        {
            _meditating = meditating;
            SetControlBlocked(_dead || _meditating);
        }

        private void HandlePlayerDied()
        {
            _dead = true;
            SetControlBlocked(true);
        }

        private void HandlePlayerRespawned()
        {
            _dead = false;
            SetControlBlocked(_meditating);
        }

        private void SetControlBlocked(bool blocked)
        {
            isControlBlocked = blocked;
            if (blocked)
            {
                _jumpBufferedUntil = float.NegativeInfinity;
                _moveDirection    = Vector2.zero;
                _targetMoveVector = Vector3.zero;
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

                // Update is skipped while blocked — freeze the locomotion animation
                // explicitly so the character doesn't keep walking in place.
                if (animator != null)
                    animator.SetFloat("Speed", 0f);
            }
        }
    }
}
