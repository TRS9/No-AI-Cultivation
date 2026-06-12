namespace CultivationGame.Core
{
    /// <summary>
    /// Temporary buff state owned exclusively by the pill/buff system.
    /// Realm-based combat scaling comes from RealmDefinition (baseDamage/baseDefense)
    /// at the point of use — it must never be written into these fields.
    /// </summary>
    public static class CultivationBuffs
    {
        public static float MeditationRateMultiplier { get; set; } = 1f;
        public static float BreakthroughBonus { get; set; } = 0f;

        // Combat Buffs (Phase 8) — multiplier base is 1, pills add/remove deltas
        public static float DamageMultiplier { get; set; } = 1f;
        public static float DefenseMultiplier { get; set; } = 1f;
        public static float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Restores all buffs to their unbuffed base values (new game, buff-system teardown).</summary>
        public static void ResetAll()
        {
            MeditationRateMultiplier = 1f;
            BreakthroughBonus = 0f;
            DamageMultiplier = 1f;
            DefenseMultiplier = 1f;
            SpeedMultiplier = 1f;
        }
    }
}
