using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class PlayerMovement : MonoBehaviour
{
    public Rigidbody rb;
    public float movespeed = 5f;
    public float sprintSpeed = 10f;
    public float acceleration = 5f; 
    private float _currentSpeed;
    public float jumpForce = 5f;
    public float sensitivity = 10f;
    public LayerMask groundLayer;
    private float maxDistanceRay = 1.1f;


    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 15f;
    public float currentStamina;
    public Slider staminaSlider;

    public float staminaRegenDelay = 1.0f;
    private float regenTimer;

    private Vector2 _moveDirection;
    private Vector3 _targetMoveVector;

    public InputActionReference move,
        jump,
        sprint;

    private void Start()
    {
        currentStamina = maxStamina;
    }
    private void Update()
    {
            _moveDirection = move.action.ReadValue<Vector2>();
        if (_moveDirection.sqrMagnitude > 0.01f)
        {
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            _targetMoveVector = forward * _moveDirection.y + right * _moveDirection.x;

            Quaternion targetRotation = Quaternion.LookRotation(_targetMoveVector);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * sensitivity);
        }
        else
        {
            _targetMoveVector = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
       if (sprint.action.IsPressed() && isGrounded() && currentStamina > 0 && _moveDirection.sqrMagnitude > 0.01f)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, sprintSpeed, acceleration * Time.fixedDeltaTime);

            currentStamina -= staminaDrainRate * Time.fixedDeltaTime;
            regenTimer = staminaRegenDelay;
        }
        else
        {
            _currentSpeed =  movespeed;

            if (regenTimer > 0)
            {
                regenTimer -= Time.fixedDeltaTime;
            }
            else
            {
                currentStamina += staminaRegenRate * Time.fixedDeltaTime;
            }
        }
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }

        rb.linearVelocity = new Vector3(_targetMoveVector.x * _currentSpeed, rb.linearVelocity.y, _targetMoveVector.z * _currentSpeed);
    }

    private void OnEnable()
    {
        jump.action.performed += HandleJump;
    }

    private void HandleJump(InputAction.CallbackContext context)
    {
        if (isGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private bool isGrounded()
    {
        if ( Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, maxDistanceRay, groundLayer))
        {
            Debug.Log(hit.collider.gameObject.name + " was hit!");
           return true;

        }
        else {
            return false;
        }
    }

    private void OnDisable()
    {
        jump.action.performed -= HandleJump;
    }

}
