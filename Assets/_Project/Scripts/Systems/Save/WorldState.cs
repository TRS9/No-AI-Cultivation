using System;
using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.Systems
{
    public static class WorldState
    {
        public static HashSet<string> CollectedIds = new();
        public static Dictionary<string, long> SpawnerTimestamps = new();

        // Guarantees static fields are reset on play mode start,
        // even when Domain Reload is disabled in Editor settings.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            CollectedIds = new HashSet<string>();
            SpawnerTimestamps = new Dictionary<string, long>();
        }

        public static bool IsCollected(string id) => CollectedIds.Contains(id);

        public static void MarkCollected(string id) => CollectedIds.Add(id);

        public static void RecordSpawnerCollection(string id)
            => SpawnerTimestamps[id] = DateTime.UtcNow.Ticks;

        public static float GetRemainingRespawn(string id, float respawnSeconds)
        {
            if (!SpawnerTimestamps.TryGetValue(id, out long ticks)) return 0f;
            var elapsed = (float)(DateTime.UtcNow - new DateTime(ticks)).TotalSeconds;
            return Mathf.Max(0f, respawnSeconds - elapsed);
        }

        public static void Clear()
        {
            CollectedIds.Clear();
            SpawnerTimestamps.Clear();
        }
    }
}
