using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.Data
{
    public struct LootResult
    {
        public ItemData item;
        public int amount;
    }

    /// <summary>
    /// Static utility that generates loot from a LootDrop table.
    /// Separated from MonoBehaviour so it can be tested in EditMode.
    /// </summary>
    public static class LootSystem
    {
        /// <summary>
        /// Rolls each entry in <paramref name="lootTable"/> and returns the items
        /// that passed their drop-chance check together with a randomised amount.
        /// </summary>
        public static List<LootResult> GenerateLoot(LootDrop[] lootTable)
        {
            var results = new List<LootResult>();
            if (lootTable == null) return results;

            foreach (LootDrop drop in lootTable)
            {
                if (drop.item == null) continue;

                float roll = Random.Range(0f, 1f);
                if (roll > drop.dropChance) continue;

                int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
                if (amount <= 0) continue;

                results.Add(new LootResult { item = drop.item, amount = amount });
            }

            return results;
        }
    }
}
