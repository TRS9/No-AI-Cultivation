using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Unity.Properties;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Player;

namespace CultivationGame.UI
{
    [Serializable]
    public struct InventorySlotData
    {
        public ItemData Item;
        public int Quantity;
        public Sprite Icon;
        public string Name;
        public ItemType ItemType;
    }

    [CreateAssetMenu(menuName = "Cultivation/UI/Inventory Data Source")]
    public class InventoryDataSource : ScriptableObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [CreateProperty]
        public List<InventorySlotData> Items { get; private set; } = new();

        [NonSerialized] public PlayerInventory PlayerInventory;

        private void Notify(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Subscribe()
        {
            GameEvents.OnInventoryChanged += RebuildItems;
        }

        public void Unsubscribe()
        {
            GameEvents.OnInventoryChanged -= RebuildItems;
        }

        public void ResetState()
        {
            Items.Clear();
            Notify(nameof(Items));
        }

        public void RebuildItems()
        {
            Items.Clear();
            if (PlayerInventory == null) return;

            foreach (var kvp in PlayerInventory.GetItems())
            {
                if (kvp.Value <= 0) continue;
                Items.Add(new InventorySlotData
                {
                    Item = kvp.Key,
                    Quantity = kvp.Value,
                    Icon = kvp.Key.icon,
                    Name = kvp.Key.name,
                    ItemType = kvp.Key.itemType
                });
            }

            Notify(nameof(Items));
        }
    }
}
