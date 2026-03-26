using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class EnemyHealthBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform fillBar;
        [SerializeField] private Transform container;

        [Header("Settings")]
        [Tooltip("Offset above the enemy to display the health bar")]
        public Vector3 offset = new Vector3(0f, 2f, 0f);
        [Tooltip("Hide after this many seconds of no damage")]
        public float hideDelay = 3f;

        private HealthSystem _healthSystem;
        private Camera _camera;
        private float _hideTimer;
        private bool _isVisible;

        private void Awake()
        {
            _healthSystem = GetComponentInParent<HealthSystem>();
            _camera = Camera.main;

            if (container != null)
                container.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged += HandleHealthChanged;
                _healthSystem.OnDied += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged -= HandleHealthChanged;
                _healthSystem.OnDied -= HandleDied;
            }
        }

        private void LateUpdate()
        {
            if (!_isVisible) return;

            // Billboard: face camera
            if (_camera != null && container != null)
            {
                container.rotation = _camera.transform.rotation;
            }

            // Auto-hide after delay
            if (_hideTimer > 0f)
            {
                _hideTimer -= Time.deltaTime;
                if (_hideTimer <= 0f)
                {
                    SetVisible(false);
                }
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (max <= 0f) return;

            float normalizedHealth = current / max;

            if (fillBar != null)
            {
                fillBar.localScale = new Vector3(normalizedHealth, 1f, 1f);
            }

            SetVisible(true);
            _hideTimer = hideDelay;
        }

        private void HandleDied()
        {
            if (fillBar != null)
                fillBar.localScale = new Vector3(0f, 1f, 1f);

            // Hide after brief moment
            _hideTimer = 0.5f;
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (container != null)
                container.gameObject.SetActive(visible);
        }
    }
}
