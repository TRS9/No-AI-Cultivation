using UnityEngine;

namespace CultivationGame.Data
{
    [CreateAssetMenu(fileName = "NewRealm", menuName = "Cultivation/Realm Definition")]
    public class RealmDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string realmName;
        public int realmIndex;
        [TextArea] public string description;

        [Header("Qi Requirements")]
        public double qiCapacity = 100;
        public double baseQiRate = 1;
        [Range(0f, 1f)] public float breakthroughSuccessRate = 1f;

        [Header("Spirit Sense")]
        [Tooltip("Maximum zoom distance for the Spirit Sense camera at this realm")]
        public float spiritSenseRange = 30f;

        [Header("Combat")]
        [Tooltip("Base attack damage at this cultivation realm")]
        public float baseDamage = 5f;
        [Tooltip("Base damage reduction at this cultivation realm")]
        public float baseDefense = 0f;

        [Header("Progression")]
        public RealmDefinition nextRealm;
    }
}
