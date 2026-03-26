using NUnit.Framework;
using UnityEngine;
using CultivationGame.Data;
using CultivationGame.Systems;

namespace CultivationGame.Tests
{
    [TestFixture]
    public class MachineInventoryTests
    {
        private class TestItemData : ItemData { }

        private TestItemData _itemA;
        private TestItemData _itemB;

        [SetUp]
        public void SetUp()
        {
            _itemA = ScriptableObject.CreateInstance<TestItemData>();
            _itemA.name = "TestItemA";

            _itemB = ScriptableObject.CreateInstance<TestItemData>();
            _itemB.name = "TestItemB";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_itemA);
            Object.DestroyImmediate(_itemB);
        }

        // ── Kapazität (Capacity) ────────────────────────────────────

        [Test]
        public void TryAdd_UntilCapacityFull_DoesNotExceedMaxCapacity()
        {
            var inv = new MachineInventory(5);

            int added = inv.TryAdd(_itemA, 5);
            Assert.AreEqual(5, added);
            Assert.AreEqual(5, inv.TotalCount());

            int overflow = inv.TryAdd(_itemA, 1);
            Assert.AreEqual(0, overflow);
            Assert.AreEqual(5, inv.TotalCount());
        }

        [Test]
        public void HasSpace_ReturnsCorrectResult()
        {
            var inv = new MachineInventory(3);

            Assert.IsTrue(inv.HasSpace(1));
            Assert.IsTrue(inv.HasSpace(3));
            Assert.IsFalse(inv.HasSpace(4));

            inv.TryAdd(_itemA, 2);

            Assert.IsTrue(inv.HasSpace(1));
            Assert.IsFalse(inv.HasSpace(2));
        }

        [Test]
        public void TryAdd_CapacityZero_TreatsAsUnlimited()
        {
            var inv = new MachineInventory(0);

            int added = inv.TryAdd(_itemA, 1000);
            Assert.AreEqual(1000, added);
            Assert.AreEqual(1000, inv.TotalCount());
            Assert.IsTrue(inv.HasSpace(999));
        }

        // ── Stacking / Item-Akkumulation ────────────────────────────

        [Test]
        public void TryAdd_SameItem_AccumulatesCount()
        {
            var inv = new MachineInventory(100);

            inv.TryAdd(_itemA, 5);
            inv.TryAdd(_itemA, 3);

            Assert.AreEqual(8, inv.Items[_itemA]);
            Assert.AreEqual(8, inv.TotalCount());
        }

        [Test]
        public void TryAdd_ExceedingCapacity_OnlyAddsWhatFits()
        {
            var inv = new MachineInventory(10);

            inv.TryAdd(_itemA, 7);
            int added = inv.TryAdd(_itemB, 5);

            Assert.AreEqual(3, added);
            Assert.AreEqual(10, inv.TotalCount());
            Assert.AreEqual(7, inv.Items[_itemA]);
            Assert.AreEqual(3, inv.Items[_itemB]);
        }

        [Test]
        public void TotalCount_MultipleDifferentItems_SumsCorrectly()
        {
            var inv = new MachineInventory(100);

            inv.TryAdd(_itemA, 4);
            inv.TryAdd(_itemB, 6);

            Assert.AreEqual(10, inv.TotalCount());
        }

        // ── Entnahme (Removal) ──────────────────────────────────────

        [Test]
        public void TryRemove_WithSufficientItems_RemovesCorrectAmount()
        {
            var inv = new MachineInventory(100);
            inv.TryAdd(_itemA, 10);

            int removed = inv.TryRemove(_itemA, 4);

            Assert.AreEqual(4, removed);
            Assert.AreEqual(6, inv.Items[_itemA]);
            Assert.AreEqual(6, inv.TotalCount());
        }

        [Test]
        public void TryRemove_ExceedingAvailable_RemovesOnlyAvailable()
        {
            var inv = new MachineInventory(100);
            inv.TryAdd(_itemA, 3);

            int removed = inv.TryRemove(_itemA, 10);

            Assert.AreEqual(3, removed);
            Assert.AreEqual(0, inv.TotalCount());
            Assert.IsFalse(inv.Items.ContainsKey(_itemA));
        }

        [Test]
        public void TryRemove_FromEmptyInventory_ReturnsZero()
        {
            var inv = new MachineInventory(100);

            int removed = inv.TryRemove(_itemA, 1);

            Assert.AreEqual(0, removed);
            Assert.AreEqual(0, inv.TotalCount());
        }

        // ── Rand-Fälle (Edge Cases) ─────────────────────────────────

        [Test]
        public void TryAdd_NullItem_ReturnsZeroAndNoException()
        {
            var inv = new MachineInventory(100);

            Assert.DoesNotThrow(() =>
            {
                int added = inv.TryAdd(null, 5);
                Assert.AreEqual(0, added);
            });

            Assert.AreEqual(0, inv.TotalCount());
        }

        [Test]
        public void TryRemove_NullItem_ReturnsZeroAndNoException()
        {
            var inv = new MachineInventory(100);
            inv.TryAdd(_itemA, 5);

            Assert.DoesNotThrow(() =>
            {
                int removed = inv.TryRemove(null, 3);
                Assert.AreEqual(0, removed);
            });

            Assert.AreEqual(5, inv.TotalCount());
        }

        [Test]
        public void SequentialAddRemoveAdd_MaintainsCorrectState()
        {
            var inv = new MachineInventory(10);

            inv.TryAdd(_itemA, 5);
            Assert.AreEqual(5, inv.TotalCount());

            inv.TryRemove(_itemA, 3);
            Assert.AreEqual(2, inv.TotalCount());

            inv.TryAdd(_itemB, 4);
            Assert.AreEqual(6, inv.TotalCount());
            Assert.AreEqual(2, inv.Items[_itemA]);
            Assert.AreEqual(4, inv.Items[_itemB]);
        }

        [Test]
        public void OnChanged_FiresOnAddAndRemove()
        {
            var inv = new MachineInventory(100);
            int fireCount = 0;
            inv.OnChanged += () => fireCount++;

            inv.TryAdd(_itemA, 3);
            Assert.AreEqual(1, fireCount);

            inv.TryRemove(_itemA, 1);
            Assert.AreEqual(2, fireCount);

            inv.Clear();
            Assert.AreEqual(3, fireCount);
        }

        [Test]
        public void Clear_EmptiesInventoryCompletely()
        {
            var inv = new MachineInventory(100);
            inv.TryAdd(_itemA, 5);
            inv.TryAdd(_itemB, 3);

            inv.Clear();

            Assert.AreEqual(0, inv.TotalCount());
            Assert.IsEmpty(inv.Items);
        }
    }
}
