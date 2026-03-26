using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CultivationGame.Data;

namespace CultivationGame.Tests
{
    /// <summary>
    /// Concrete ItemData subclass used only in tests (ItemData is abstract).
    /// </summary>
    internal sealed class TestItemData : ItemData { }

    [TestFixture]
    public class LootSystemTests
    {
        private TestItemData CreateItem(string itemName)
        {
            var item = ScriptableObject.CreateInstance<TestItemData>();
            item.name = itemName;
            return item;
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any ScriptableObjects created during the test
        }

        // ------------------------------------------------------------------
        // Null / empty table
        // ------------------------------------------------------------------

        [Test]
        public void GenerateLoot_NullTable_ReturnsEmptyList()
        {
            List<LootResult> results = LootSystem.GenerateLoot(null);
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void GenerateLoot_EmptyTable_ReturnsEmptyList()
        {
            List<LootResult> results = LootSystem.GenerateLoot(new LootDrop[0]);
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        // ------------------------------------------------------------------
        // Guaranteed drops (dropChance == 1)
        // ------------------------------------------------------------------

        [Test]
        public void GenerateLoot_GuaranteedDrop_AlwaysReturnsItem()
        {
            var item = CreateItem("GuaranteedItem");
            var table = new[]
            {
                new LootDrop { item = item, minAmount = 1, maxAmount = 1, dropChance = 1f }
            };

            // Run multiple times to rule out randomness
            for (int i = 0; i < 50; i++)
            {
                List<LootResult> results = LootSystem.GenerateLoot(table);
                Assert.AreEqual(1, results.Count, $"Iteration {i}: expected exactly 1 result");
                Assert.AreEqual(item, results[0].item);
                Assert.AreEqual(1, results[0].amount);
            }

            Object.DestroyImmediate(item);
        }

        // ------------------------------------------------------------------
        // Null item in entry is skipped
        // ------------------------------------------------------------------

        [Test]
        public void GenerateLoot_NullItem_IsSkipped()
        {
            var table = new[]
            {
                new LootDrop { item = null, minAmount = 1, maxAmount = 1, dropChance = 1f }
            };

            List<LootResult> results = LootSystem.GenerateLoot(table);
            Assert.AreEqual(0, results.Count);
        }

        // ------------------------------------------------------------------
        // Amount within min/max range
        // ------------------------------------------------------------------

        [Test]
        public void GenerateLoot_AmountWithinMinMax()
        {
            var item = CreateItem("RangeItem");
            var table = new[]
            {
                new LootDrop { item = item, minAmount = 2, maxAmount = 5, dropChance = 1f }
            };

            for (int i = 0; i < 100; i++)
            {
                List<LootResult> results = LootSystem.GenerateLoot(table);
                Assert.AreEqual(1, results.Count);
                Assert.GreaterOrEqual(results[0].amount, 2, $"Iteration {i}");
                Assert.LessOrEqual(results[0].amount, 5, $"Iteration {i}");
            }

            Object.DestroyImmediate(item);
        }

        // ------------------------------------------------------------------
        // Multiple drops
        // ------------------------------------------------------------------

        [Test]
        public void GenerateLoot_MultipleGuaranteedDrops_ReturnsAll()
        {
            var item1 = CreateItem("Item1");
            var item2 = CreateItem("Item2");
            var table = new[]
            {
                new LootDrop { item = item1, minAmount = 1, maxAmount = 1, dropChance = 1f },
                new LootDrop { item = item2, minAmount = 3, maxAmount = 3, dropChance = 1f }
            };

            List<LootResult> results = LootSystem.GenerateLoot(table);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(item1, results[0].item);
            Assert.AreEqual(1, results[0].amount);
            Assert.AreEqual(item2, results[1].item);
            Assert.AreEqual(3, results[1].amount);

            Object.DestroyImmediate(item1);
            Object.DestroyImmediate(item2);
        }

        // ------------------------------------------------------------------
        // EnemyData integration
        // ------------------------------------------------------------------

        [Test]
        public void GenerateLoot_FromEnemyData_UsesLootTable()
        {
            var item = CreateItem("EnemyItem");
            var enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.lootTable = new[]
            {
                new LootDrop { item = item, minAmount = 1, maxAmount = 2, dropChance = 1f }
            };

            List<LootResult> results = LootSystem.GenerateLoot(enemyData.lootTable);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(item, results[0].item);
            Assert.GreaterOrEqual(results[0].amount, 1);
            Assert.LessOrEqual(results[0].amount, 2);

            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(item);
        }

        [Test]
        public void GenerateLoot_EnemyDataNullLootTable_ReturnsEmpty()
        {
            var enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.lootTable = null;

            List<LootResult> results = LootSystem.GenerateLoot(enemyData.lootTable);
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);

            Object.DestroyImmediate(enemyData);
        }
    }
}
