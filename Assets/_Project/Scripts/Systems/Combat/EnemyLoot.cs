using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    public class EnemyLoot : MonoBehaviour
    {
        [Header("References")]
        public EnemyData enemyData;

        private HealthSystem _healthSystem;

        private void Awake()
        {
            _healthSystem = GetComponent<HealthSystem>();
        }

        private void OnEnable()
        {
            if (_healthSystem != null)
                _healthSystem.OnDied += DropLoot;
        }

        private void OnDisable()
        {
            if (_healthSystem != null)
                _healthSystem.OnDied -= DropLoot;
        }

        private void DropLoot()
        {
            if (enemyData == null) return;

            var loot = LootSystem.GenerateLoot(enemyData.lootTable);
            if (loot.Count == 0) return;

            // Cache the player reference once for the whole drop sequence
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            IInventory inventory = player != null ? player.GetComponent<IInventory>() : null;

            foreach (LootResult result in loot)
            {
                GameDataEvents.RaiseLootDropped(result.item, result.amount, transform.position);

                // Batch add — one inventory event per stack instead of one per item.
                inventory?.AddItem(result.item, result.amount);
            }
        }
    }
}
