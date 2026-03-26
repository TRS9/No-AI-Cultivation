using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    public class PillBuffSystem : MonoBehaviour
    {
        public static PillBuffSystem Instance { get; private set; }

        // Tolerance tracking: pill asset name → session use count
        private readonly Dictionary<string, int> _useCount = new();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            CultivationBuffs.MeditationRateMultiplier = 1f;
            CultivationBuffs.BreakthroughBonus = 0f;
        }

        private void OnEnable()  => GameDataEvents.OnPillConsumed += HandlePillConsumed;
        private void OnDisable() => GameDataEvents.OnPillConsumed -= HandlePillConsumed;

        private void HandlePillConsumed(PillData pill)
        {
            string key = pill.name;
            _useCount.TryGetValue(key, out int used);

            float effectiveness = pill.maxDailyUses <= 0
                ? 1f
                : Mathf.Max(0f, 1f - (float)used / pill.maxDailyUses);

            _useCount[key] = used + 1;

            if (pill.qiBoost > 0)
                GameEvents.RaiseAddQi(pill.qiBoost * effectiveness);

            if (pill.cultivationSpeedMultiplier > 1f && pill.buffDuration > 0f)
                StartCoroutine(ApplySpeedBuff(pill.cultivationSpeedMultiplier, pill.buffDuration, effectiveness, pill.pillName));

            if (pill.breakthroughBonus > 0f && pill.buffDuration > 0f)
                StartCoroutine(ApplyBreakthroughBuff(pill.breakthroughBonus, pill.buffDuration, effectiveness, pill.pillName));

            // Combat buffs (Phase 8)
            if (pill.damageBoost > 0f && pill.buffDuration > 0f)
                StartCoroutine(ApplyDamageBuff(pill.damageBoost, pill.buffDuration, effectiveness, pill.pillName));

            if (pill.defenseBoost > 0f && pill.buffDuration > 0f)
                StartCoroutine(ApplyDefenseBuff(pill.defenseBoost, pill.buffDuration, effectiveness, pill.pillName));

            if (pill.speedBoost > 0f && pill.buffDuration > 0f)
                StartCoroutine(ApplyCombatSpeedBuff(pill.speedBoost, pill.buffDuration, effectiveness, pill.pillName));

            GameDataEvents.RaisePillEffectsApplied(pill, effectiveness);
        }

        private IEnumerator ApplySpeedBuff(float multiplier, float duration, float effectiveness, string pillName)
        {
            float bonus = (multiplier - 1f) * effectiveness;
            CultivationBuffs.MeditationRateMultiplier += bonus;
            GameEvents.RaiseBuffStarted("Meditation", pillName, duration);
            yield return new WaitForSeconds(duration);
            CultivationBuffs.MeditationRateMultiplier -= bonus;
            GameEvents.RaiseBuffExpired("Meditation", pillName);
        }

        private IEnumerator ApplyBreakthroughBuff(float bonus, float duration, float effectiveness, string pillName)
        {
            float applied = bonus * effectiveness;
            CultivationBuffs.BreakthroughBonus += applied;
            GameEvents.RaiseBuffStarted("Breakthrough", pillName, duration);
            yield return new WaitForSeconds(duration);
            CultivationBuffs.BreakthroughBonus -= applied;
            GameEvents.RaiseBuffExpired("Breakthrough", pillName);
        }

        private IEnumerator ApplyDamageBuff(float bonus, float duration, float effectiveness, string pillName)
        {
            float applied = bonus * effectiveness;
            CultivationBuffs.DamageMultiplier += applied;
            GameEvents.RaiseBuffStarted("Damage", pillName, duration);
            yield return new WaitForSeconds(duration);
            CultivationBuffs.DamageMultiplier -= applied;
            GameEvents.RaiseBuffExpired("Damage", pillName);
        }

        private IEnumerator ApplyDefenseBuff(float bonus, float duration, float effectiveness, string pillName)
        {
            float applied = bonus * effectiveness;
            CultivationBuffs.DefenseMultiplier += applied;
            GameEvents.RaiseBuffStarted("Defense", pillName, duration);
            yield return new WaitForSeconds(duration);
            CultivationBuffs.DefenseMultiplier -= applied;
            GameEvents.RaiseBuffExpired("Defense", pillName);
        }

        private IEnumerator ApplyCombatSpeedBuff(float bonus, float duration, float effectiveness, string pillName)
        {
            float applied = bonus * effectiveness;
            CultivationBuffs.SpeedMultiplier += applied;
            GameEvents.RaiseBuffStarted("Speed", pillName, duration);
            yield return new WaitForSeconds(duration);
            CultivationBuffs.SpeedMultiplier -= applied;
            GameEvents.RaiseBuffExpired("Speed", pillName);
        }
    }
}
