using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// A storage buffer between production steps.
    /// Has both input and output inventories (same backing store).
    /// Spirit Pipes can push items in and pull items out.
    /// </summary>
    public class StorageContainer : MonoBehaviour, IInteractable
    {
        [Header("Machine Configuration")]
        [SerializeField] private MachineData machineData;

        [Header("Storage Settings")]
        [SerializeField] private int capacity = 500;

        [Header("Filter (optional)")]
        [Tooltip("If set, only these items are accepted. Leave empty for any item.")]
        [SerializeField] private ItemData[] acceptedItems;

        private MachineInventory _inventory;

        // --- Public API ---
        // Storage uses a single inventory for both input and output
        public MachineData MachineData => machineData;
        public MachineInventory InputInventory => _inventory;
        public MachineInventory OutputInventory => _inventory;
        public int Capacity => capacity;

        private void Awake()
        {
            _inventory = new MachineInventory(capacity);
        }

        public void SetMachineData(MachineData data)
        {
            machineData = data;
        }

        public void Interact(GameObject user)
        {
            GameDataEvents.RaiseMachineInteracted(this);
        }

        /// <summary>
        /// Check if this storage accepts a given item (respects filter).
        /// </summary>
        public bool AcceptsItem(ItemData item)
        {
            if (acceptedItems == null || acceptedItems.Length == 0) return true;
            foreach (var accepted in acceptedItems)
                if (accepted == item) return true;
            return false;
        }
    }
}
