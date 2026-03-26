using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// A simple item buffer used by machines, storage containers, and pipes.
    /// Tracks items as a Dictionary&lt;ItemData, int&gt; with an optional capacity limit.
    /// </summary>
    [Serializable]
    public class MachineInventory
    {
        [SerializeField] private int maxCapacity = 100;

        private Dictionary<ItemData, int> _items = new();

        public int MaxCapacity => maxCapacity;
        public Dictionary<ItemData, int> Items => _items;

        public event Action OnChanged;

        public MachineInventory() { }

        public MachineInventory(int capacity)
        {
            maxCapacity = capacity;
        }

        public int TotalCount()
        {
            int total = 0;
            foreach (var kv in _items) total += kv.Value;
            return total;
        }

        public bool HasSpace(int amount = 1)
        {
            return maxCapacity <= 0 || TotalCount() + amount <= maxCapacity;
        }

        public bool HasItem(ItemData item, int amount = 1)
        {
            return item != null && _items.TryGetValue(item, out int count) && count >= amount;
        }

        /// <summary>
        /// Tries to add items. Returns the number actually added (may be less if capacity is reached).
        /// </summary>
        public int TryAdd(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return 0;

            int canAdd = maxCapacity > 0
                ? Mathf.Min(amount, maxCapacity - TotalCount())
                : amount;

            if (canAdd <= 0) return 0;

            if (_items.ContainsKey(item))
                _items[item] += canAdd;
            else
                _items[item] = canAdd;

            OnChanged?.Invoke();
            return canAdd;
        }

        /// <summary>
        /// Tries to remove items. Returns the number actually removed.
        /// </summary>
        public int TryRemove(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return 0;
            if (!_items.TryGetValue(item, out int count)) return 0;

            int removed = Mathf.Min(amount, count);
            _items[item] -= removed;
            if (_items[item] <= 0) _items.Remove(item);

            OnChanged?.Invoke();
            return removed;
        }

        /// <summary>
        /// Returns the first available item (for pipes that transfer any item).
        /// </summary>
        public ItemData GetFirstItem()
        {
            foreach (var kv in _items)
                if (kv.Value > 0) return kv.Key;
            return null;
        }

        public void Clear()
        {
            _items.Clear();
            OnChanged?.Invoke();
        }

        public Dictionary<ItemData, int> GetSnapshot()
        {
            return new Dictionary<ItemData, int>(_items);
        }

        public void LoadFrom(Dictionary<ItemData, int> data)
        {
            _items = new Dictionary<ItemData, int>(data);
            OnChanged?.Invoke();
        }
    }
}
