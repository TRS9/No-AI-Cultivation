using UnityEngine;
using UnityEngine.UIElements;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIDocument uiDocument;

        private ProgressBar _healthBar;
        private Label _healthLabel;
        private VisualElement _healthContainer;

        private float _currentHealth;
        private float _maxHealth;

        private void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += HandleHealthChanged;
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= HandleHealthChanged;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        public void InitializeUI(VisualElement root)
        {
            _healthContainer = root.Q<VisualElement>("HealthContainer");
            _healthBar = root.Q<ProgressBar>("HealthBar");
            _healthLabel = root.Q<Label>("HealthLabel");

            UpdateDisplay();
        }

        private void HandleHealthChanged(float current, float max)
        {
            _currentHealth = current;
            _maxHealth = max;
            UpdateDisplay();
        }

        private void HandlePlayerDied()
        {
            _currentHealth = 0f;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            float percent = _maxHealth > 0f ? (_currentHealth / _maxHealth) * 100f : 0f;

            if (_healthBar != null)
                _healthBar.value = percent;

            if (_healthLabel != null)
                _healthLabel.text = $"HP {_currentHealth:F0}/{_maxHealth:F0}";
        }
    }
}
