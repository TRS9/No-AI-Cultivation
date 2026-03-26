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
            if (enemyData == null || enemyData.lootTable == null) return;

            foreach (LootDrop drop in enemyData.lootTable)
            {
                if (drop.item == null) continue;

                float roll = Random.Range(0f, 1f);
                if (roll > drop.dropChance) continue;

                int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
                if (amount <= 0) continue;

                GameDataEvents.RaiseLootDropped(drop.item, amount, transform.position);

                // Try to add directly to player inventory if nearby
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    var inventory = player.GetComponent<IInventory>();
                    if (inventory != null)
                    {
                        for (int i = 0; i < amount; i++)
                        {
                            inventory.AddItem(drop.item);
                        }
                        Debug.Log($"Loot: {amount}x {drop.item.name} added to inventory.");
                    }
                }
            }
        }
    }
}
