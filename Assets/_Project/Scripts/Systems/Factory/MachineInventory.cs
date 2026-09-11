using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        [SerializeField] [Tooltip("Maximum total item count this inventory can hold across all item types.")] private int maxCapacity = 100;

        private readonly Dictionary<ItemData, int> _items = new();
        private ReadOnlyDictionary<ItemData, int> _readOnlyItems;
        private int _totalCount;

        public int MaxCapacity => maxCapacity;
        public IReadOnlyDictionary<ItemData, int> Items =>
            _readOnlyItems ??= new ReadOnlyDictionary<ItemData, int>(_items);

        public event Action OnChanged;

        public MachineInventory() { }

        public MachineInventory(int capacity)
        {
            maxCapacity = capacity;
        }

        public int TotalCount()
        {
            return _totalCount;
        }

        public bool HasSpace(int amount = 1)
        {
            return amount >= 0 && (long)_totalCount + amount <=
                (maxCapacity > 0 ? maxCapacity : int.MaxValue);
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
                : Mathf.Min(amount, int.MaxValue - _totalCount);

            if (canAdd <= 0) return 0;

            if (_items.ContainsKey(item))
                _items[item] += canAdd;
            else
                _items[item] = canAdd;

            _totalCount += canAdd;
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

            _totalCount -= removed;
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
            _totalCount = 0;
            OnChanged?.Invoke();
        }

        public Dictionary<ItemData, int> GetSnapshot()
        {
            return new Dictionary<ItemData, int>(_items);
        }

        public void LoadFrom(Dictionary<ItemData, int> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            long total = 0;
            foreach (var entry in data)
                if (entry.Key != null && entry.Value > 0) total += entry.Value;
            if (total > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(data));

            _items.Clear();
            foreach (var entry in data)
                if (entry.Key != null && entry.Value > 0) _items.Add(entry.Key, entry.Value);
            _totalCount = (int)total;
            OnChanged?.Invoke();
        }
    }
}
