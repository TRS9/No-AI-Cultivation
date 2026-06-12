using System;
using UnityEngine;

namespace CultivationGame.Core
{
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public event Action<float, float> OnHealthChanged;
        public event Action OnDied;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void Initialize(float max)
        {
            maxHealth = max;
            CurrentHealth = max;
            IsDead = false;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(float damage)
        {
            if (IsDead || damage <= 0f) return;

            CurrentHealth = Mathf.Max(CurrentHealth - damage, 0f);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
            {
                IsDead = true;
                OnDied?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        /// <summary>Brings a dead entity back to life at full health (player respawn).</summary>
        public void Revive()
        {
            IsDead = false;
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void SetMaxHealth(float newMax, bool healToFull = false)
        {
            maxHealth = newMax;
            if (healToFull)
                CurrentHealth = maxHealth;
            else
                CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
