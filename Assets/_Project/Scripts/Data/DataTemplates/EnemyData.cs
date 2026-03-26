using System;
using UnityEngine;

namespace CultivationGame.Data
{
    [Serializable]
    public struct LootDrop
    {
        public ItemData item;
        public int minAmount;
        public int maxAmount;
        [Range(0f, 1f)] public float dropChance;
    }

    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Cultivation/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Stats")]
        public float maxHealth = 50f;
        public float damage = 10f;
        [Tooltip("Seconds between attacks")]
        public float attackCooldown = 1.5f;
        public float moveSpeed = 3f;

        [Header("AI Ranges")]
        [Tooltip("Distance at which the enemy detects the player")]
        public float detectionRange = 10f;
        [Tooltip("Distance at which the enemy can attack")]
        public float attackRange = 2f;
        [Tooltip("Max distance from spawn point for patrol")]
        public float patrolRadius = 5f;
        [Tooltip("Distance beyond which the enemy gives up chasing and returns")]
        public float leashRange = 20f;

        [Header("Requirements")]
        [Tooltip("Minimum cultivation realm needed to deal damage to this enemy")]
        public RealmDefinition minimumRealm;

        [Header("Loot")]
        public LootDrop[] lootTable;

        [Header("Visuals")]
        public GameObject prefab;
    }
}
