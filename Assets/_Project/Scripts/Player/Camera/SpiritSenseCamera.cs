using UnityEngine;
using UnityEngine.InputSystem;

namespace CultivationGame.Player
{
    public class SpiritSenseCamera : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float panSpeed = 20f;

        [Header("Orbit")]
        [SerializeField] private float orbitSensitivity = 2f;
        [SerializeField] private float minPitch = 10f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 50f;

        [Header("Initial")]
        [SerializeField] private float initialHeight = 20f;
        [SerializeField] private float initialPitch = 60f;

        [Header("Input")]
        public InputActionReference panAction;
        public InputActionReference orbitAction;
        public InputActionReference orbitActivateAction;
        public InputActionReference zoomAction;

        private Vector3 _focalPoint;
        private float _yaw;
        private float _pitch;
        private float _currentZoom;
        private Camera _camera;
        private bool _isActive;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (!_isActive) return;

            HandlePan();
            HandleOrbit();
            HandleZoom();
            ApplyTransform();
        }

        private void HandlePan()
        {
            if (panAction == null) return;
            Vector2 input = panAction.action.ReadValue<Vector2>();
            if (input.sqrMagnitude < 0.01f) return;

            Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 flatRight = new Vector3(transform.right.x, 0f, transform.right.z).normalized;
            Vector3 panDelta = (flatForward * input.y + flatRight * input.x) * panSpeed * Time.deltaTime;
            _focalPoint += panDelta;
        }

        private void HandleOrbit()
        {
            if (orbitAction == null || orbitActivateAction == null) return;
            if (!orbitActivateAction.action.IsPressed()) return;

            Vector2 delta = orbitAction.action.ReadValue<Vector2>();
            _yaw += delta.x * orbitSensitivity;
            _pitch -= delta.y * orbitSensitivity;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        private void HandleZoom()
        {
            if (zoomAction == null) return;
            float scrollDelta = zoomAction.action.ReadValue<float>();
            if (Mathf.Abs(scrollDelta) < 0.01f) return;

            _currentZoom -= scrollDelta * zoomSpeed * Time.deltaTime;
            _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -_currentZoom);
            transform.position = _focalPoint + offset;
            transform.LookAt(_focalPoint);
        }

        public void Initialize(Vector3 playerPosition, float playerYaw)
        {
            _focalPoint = playerPosition;
            _yaw = playerYaw;
            _pitch = initialPitch;
            _currentZoom = initialHeight;
            ApplyTransform();
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

        public void SetElevation(float y)
        {
            _focalPoint.y = y;
        }

        public Camera GetCamera() => _camera;
    }
}
