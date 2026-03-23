using UnityEngine;
using UnityEngine.InputSystem;

namespace CultivationGame.Player
{
    public class ActionCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Orbit Settings")]
        [SerializeField] private float distance = 5f;
        [SerializeField] private float heightOffset = 1.5f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 60f;

        [Header("Smoothing")]
        [SerializeField] private float followSmoothTime = 0.1f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.2f;
        [SerializeField] private LayerMask collisionLayers;

        [Header("Input")]
        public InputActionReference lookAction;

        private float _yaw;
        private float _pitch = 15f;
        private float _currentDistance;
        private Vector3 _smoothVelocity;
        private Camera _camera;
        private bool _isActive = true;

        public float Yaw => _yaw;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _currentDistance = distance;
        }

        private void Start()
        {
            if (target != null)
            {
                _yaw = target.eulerAngles.y;
            }
        }

        private void LateUpdate()
        {
            if (!_isActive || target == null) return;

            ReadInput();
            UpdatePosition();
        }

        private void ReadInput()
        {
            if (lookAction == null) return;
            Vector2 lookDelta = lookAction.action.ReadValue<Vector2>();
            _yaw += lookDelta.x * mouseSensitivity;
            _pitch -= lookDelta.y * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        private void UpdatePosition()
        {
            Vector3 pivot = target.position + Vector3.up * heightOffset;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPos = pivot - rotation * Vector3.forward * distance;

            // Collision avoidance: pull camera forward if obstructed
            float adjustedDistance = distance;
            Vector3 direction = desiredPos - pivot;
            if (Physics.SphereCast(pivot, collisionRadius, direction.normalized, out RaycastHit hit,
                    distance, collisionLayers))
            {
                adjustedDistance = hit.distance;
            }

            _currentDistance = Mathf.Lerp(_currentDistance, adjustedDistance, 10f * Time.deltaTime);
            Vector3 finalPos = pivot - rotation * Vector3.forward * _currentDistance;

            transform.position = Vector3.SmoothDamp(transform.position, finalPos,
                ref _smoothVelocity, followSmoothTime);
            transform.rotation = rotation;
        }

        public void SetEnabled(bool enabled)
        {
            _isActive = enabled;
            if (_camera != null)
                _camera.enabled = enabled;
        }

        public (Vector3 position, Quaternion rotation) GetCurrentState()
        {
            return (transform.position, transform.rotation);
        }

        public Camera GetCamera() => _camera;
    }
}
