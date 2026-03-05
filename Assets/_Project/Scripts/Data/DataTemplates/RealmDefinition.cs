using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data
{
    [CreateAssetMenu(fileName = "NewRealm", menuName = "Cultivation/Realm Definition")]
    public class RealmDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string realmName;
        public int realmIndex;
        public RealmSubStage subStage;
        [TextArea] public string description;

        [Header("Qi Requirements")]
        public double qiCapacity = 100;
        public double baseQiRate = 1;
        [Range(0f, 1f)] public float breakthroughSuccessRate = 1f;

        [Header("Progression")]
        public RealmDefinition nextRealm;

        [Header("Combat")]
        public float baseCombatPower;
    }
}
