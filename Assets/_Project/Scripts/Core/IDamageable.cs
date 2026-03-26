using UnityEngine;

namespace CultivationGame.Core
{
    public interface IDamageable
    {
        void TakeDamage(float damage, GameObject attacker);
        bool IsDead { get; }
    }
}
