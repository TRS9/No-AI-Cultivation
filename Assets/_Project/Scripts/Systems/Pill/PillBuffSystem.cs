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
                StartCoroutine(ApplySpeedBuff(pill.cultivationSpeedMultiplier, pill.buffDuration, effectiveness));

            if (pill.breakthroughBonus > 0f && pill.buffDuration > 0f)
                StartCoroutine(ApplyBreakthroughBuff(pill.breakthroughBonus, pill.buffDuration, effectiveness));

            GameDataEvents.RaisePillEffectsApplied(pill, effectiveness);
        }

        private IEnumerator ApplySpeedBuff(float multiplier, float duration, float effectiveness)
        {
            float bonus = (multiplier - 1f) * effectiveness;
            CultivationBuffs.MeditationRateMultiplier += bonus;
            yield return new WaitForSeconds(duration);
            CultivationBuffs.MeditationRateMultiplier -= bonus;
        }

        private IEnumerator ApplyBreakthroughBuff(float bonus, float duration, float effectiveness)
        {
            float applied = bonus * effectiveness;
            CultivationBuffs.BreakthroughBonus += applied;
            yield return new WaitForSeconds(duration);
            CultivationBuffs.BreakthroughBonus -= applied;
        }
    }
}
