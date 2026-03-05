using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;

namespace CultivationGame.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Physics & Movement")]
        public Rigidbody rb;
        public float moveSpeed = 5f;
        public float sprintSpeed = 10f;
        public float acceleration = 5f;
        public float jumpForce = 5f;
        public float rotationSpeed = 600f;
        public LayerMask groundLayer;
        public float maxDistanceRay = 1.1f;

        [Header("Stamina System")]
        public float maxStamina = 100f;
        public float staminaDrainRate = 20f;
        public float staminaRegenRate = 15f;
        public float staminaRegenDelay = 1.0f;
        public float currentStamina;

        private float _currentSpeed;
        private float _regenTimer;
        private Vector2 _moveDirection;
        private Vector3 _targetMoveVector;
        private Vector3 _smoothedDirection;
        private bool _wasGrounded;
        private float _airborneTime;
        private const float LandingResetThreshold = 0.6f;

        [Header("Animation")]
        public Animator animator;

        [Header("Input References")]
        public InputActionReference move;
        public InputActionReference jump;
        public InputActionReference sprint;

        private bool isControlBlocked;

        private void Start()
        {
            currentStamina = maxStamina;
            GameEvents.RaiseStaminaChanged(currentStamina, maxStamina);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void OnEnable()
        {
            jump.action.performed += HandleJump;
            GameEvents.OnMeditationToggled += HandleMeditationBlock;
        }

        private void OnDisable()
        {
            jump.action.performed -= HandleJump;
            GameEvents.OnMeditationToggled -= HandleMeditationBlock;
        }

        private void Update()
        {
            if (isControlBlocked) return;
            ReadInput();
            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            bool isGrounded = IsGrounded();
            ComputeMoveVector();
            HandleStaminaAndSpeed(isGrounded);
            ApplyMovement();
            ApplyRotation();
        }

        private void ReadInput()
        {
            _moveDirection = move.action.ReadValue<Vector2>();
        }

        // Runs in FixedUpdate so direction is computed once per physics tick,
        // preventing camera-feedback oscillation that occurs at variable Update rate.
        private void ComputeMoveVector()
        {
            if (_moveDirection.sqrMagnitude > 0.01f)
            {
                var cam = Camera.main;
                if (cam == null) return;

                Vector3 forward = cam.transform.forward;
                Vector3 right   = cam.transform.right;

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
            if (!isGrounded)
            {
                _airborneTime += Time.fixedDeltaTime;
            }
            else if (!_wasGrounded && _airborneTime >= LandingResetThreshold)
            {
                // Dampen speed on landing instead of freezing — smooth transition back to walk
                _currentSpeed = Mathf.Min(_currentSpeed, moveSpeed * 0.3f);
            }

            if (isGrounded)
                _airborneTime = 0f;

            _wasGrounded = isGrounded;

            bool isMoving    = _moveDirection.sqrMagnitude > 0.01f;
            bool isSprinting = sprint.action.IsPressed() && currentStamina > 0 && isMoving && isGrounded;

            if (isSprinting)
            {
                _currentSpeed  = Mathf.MoveTowards(_currentSpeed, sprintSpeed, acceleration * Time.fixedDeltaTime);
                currentStamina -= staminaDrainRate * Time.fixedDeltaTime;
                _regenTimer    = staminaRegenDelay;
            }
            else
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, moveSpeed, acceleration * Time.fixedDeltaTime);

                if (_regenTimer > 0)
                    _regenTimer -= Time.fixedDeltaTime;
                else
                    currentStamina += staminaRegenRate * Time.fixedDeltaTime;
            }

            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            GameEvents.RaiseStaminaChanged(currentStamina, maxStamina);
        }

        private void ApplyMovement()
        {
            rb.linearVelocity = new Vector3(
                _targetMoveVector.x * _currentSpeed,
                rb.linearVelocity.y,
                _targetMoveVector.z * _currentSpeed
            );
        }

        private void HandleJump(InputAction.CallbackContext context)
        {
            if (IsGrounded())
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                if (animator != null)
                    animator.SetTrigger("Jump");
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            Vector3 horizontalVelocity  = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float   currentSpeedMagnitude = horizontalVelocity.magnitude;

            animator.SetFloat("Speed", currentSpeedMagnitude);
            animator.SetBool("IsGrounded", IsGrounded());
        }

        public bool IsGrounded()
        {
            return Physics.Raycast(transform.position, Vector3.down, out _, maxDistanceRay, groundLayer);
        }

        private void HandleMeditationBlock(bool isMeditating)
        {
            isControlBlocked = isMeditating;
            if (isMeditating)
            {
                _moveDirection    = Vector2.zero;
                _targetMoveVector = Vector3.zero;
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }
        }
    }
}
