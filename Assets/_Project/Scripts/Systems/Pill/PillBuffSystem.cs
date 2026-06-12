using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Applies temporary pill buffs to the shared CultivationBuffs state.
    /// Buffs are tracked as timestamped records (not coroutines) so they are
    /// reliably reverted when they expire — and force-reverted in OnDestroy,
    /// which prevents permanently stuck buffs when a scene change destroys
    /// this system mid-buff.
    /// </summary>
    public class PillBuffSystem : MonoBehaviour
    {
        public static PillBuffSystem Instance { get; private set; }

        private enum BuffType { Meditation, Breakthrough, Damage, Defense, Speed }

        private class ActiveBuff
        {
            public BuffType Type;
            public float Delta;
            public float EndTime;
            public string BuffName;
            public string PillName;
        }

        // Tolerance tracking: pill asset name → session use count
        private readonly Dictionary<string, int> _useCount = new();
        private readonly List<ActiveBuff> _activeBuffs = new();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            // This system is the sole owner of CultivationBuffs — start from a clean slate.
            CultivationBuffs.ResetAll();
        }

        private void OnEnable()  => GameDataEvents.OnPillConsumed += HandlePillConsumed;
        private void OnDisable() => GameDataEvents.OnPillConsumed -= HandlePillConsumed;

        private void OnDestroy()
        {
            // Revert everything still active so the static buff state never leaks
            // into the next scene with no system left to expire it.
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
                ExpireBuff(i);

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            float now = Time.time;
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (now >= _activeBuffs[i].EndTime)
                    ExpireBuff(i);
            }
        }

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
                StartBuff(BuffType.Meditation, "Meditation",
                    (pill.cultivationSpeedMultiplier - 1f) * effectiveness, pill);

            if (pill.breakthroughBonus > 0f && pill.buffDuration > 0f)
                StartBuff(BuffType.Breakthrough, "Breakthrough",
                    pill.breakthroughBonus * effectiveness, pill);

            // Combat buffs (Phase 8)
            if (pill.damageBoost > 0f && pill.buffDuration > 0f)
                StartBuff(BuffType.Damage, "Damage", pill.damageBoost * effectiveness, pill);

            if (pill.defenseBoost > 0f && pill.buffDuration > 0f)
                StartBuff(BuffType.Defense, "Defense", pill.defenseBoost * effectiveness, pill);

            if (pill.speedBoost > 0f && pill.buffDuration > 0f)
                StartBuff(BuffType.Speed, "Speed", pill.speedBoost * effectiveness, pill);

            GameDataEvents.RaisePillEffectsApplied(pill, effectiveness);
        }

        private void StartBuff(BuffType type, string buffName, float delta, PillData pill)
        {
            ApplyDelta(type, delta);

            _activeBuffs.Add(new ActiveBuff
            {
                Type = type,
                Delta = delta,
                EndTime = Time.time + pill.buffDuration,
                BuffName = buffName,
                PillName = pill.pillName,
            });

            GameEvents.RaiseBuffStarted(buffName, pill.pillName, pill.buffDuration);
        }

        private void ExpireBuff(int index)
        {
            var buff = _activeBuffs[index];
            _activeBuffs.RemoveAt(index);
            ApplyDelta(buff.Type, -buff.Delta);
            GameEvents.RaiseBuffExpired(buff.BuffName, buff.PillName);
        }

        private static void ApplyDelta(BuffType type, float delta)
        {
            switch (type)
            {
                case BuffType.Meditation:   CultivationBuffs.MeditationRateMultiplier += delta; break;
                case BuffType.Breakthrough: CultivationBuffs.BreakthroughBonus        += delta; break;
                case BuffType.Damage:       CultivationBuffs.DamageMultiplier         += delta; break;
                case BuffType.Defense:      CultivationBuffs.DefenseMultiplier        += delta; break;
                case BuffType.Speed:        CultivationBuffs.SpeedMultiplier          += delta; break;
            }
        }
    }
}
