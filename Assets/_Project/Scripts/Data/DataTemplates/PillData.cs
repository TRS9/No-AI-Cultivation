using UnityEngine;

namespace CultivationGame.Data
{
    public enum PillGrade { Mortal, Earth, Heaven, Divine }

    [CreateAssetMenu(fileName = "NewPill", menuName = "Cultivation/Pill Data")]
    public class PillData : ItemData
    {
        [Header("Identity")]
        public string pillName;
        public Color pillColor = Color.white;
        public PillGrade grade;

        [Header("Effects")]
        [Tooltip("1-5, matches the Tier hierarchy in the GAMEPLAN")]
        public int pillTier = 1;
        [Tooltip("Immediate Qi gain on consumption")]
        public double qiBoost;
        [Tooltip("Multiplier applied to meditationQiRate for buffDuration seconds (1 = no bonus)")]
        public float cultivationSpeedMultiplier = 1f;
        [Tooltip("Additive bonus to breakthroughSuccessRate for buffDuration seconds")]
        public float breakthroughBonus;
        [Tooltip("Duration of temporal buffs in seconds")]
        public float buffDuration;

        [Header("Tolerance & Lore")]
        [Tooltip("Maximum beneficial uses per session; additional uses have diminishing effect")]
        public int maxDailyUses = 1;
        [Tooltip("Flavor text describing side effects")]
        [TextArea] public string sideEffects;
    }
}
