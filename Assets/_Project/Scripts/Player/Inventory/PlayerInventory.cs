using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Player
{
    public class PlayerInventory : MonoBehaviour, IInventory
    {
        public PlayerStats playerStats;

        private Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();

        public void AddItem(ItemData item)
        {
            if (item == null) return;

            if (items.ContainsKey(item))
            {
                items[item]++;
            }
            else
            {
                items.Add(item, 1);
            }

            GameEvents.RaiseInventoryChanged();
        }

        public void UsePill(PillData pill)
        {
            if (!items.ContainsKey(pill) || items[pill] <= 0) return;

            items[pill]--;
            if (items[pill] <= 0) items.Remove(pill);

            GameDataEvents.RaisePillConsumed(pill);
            GameEvents.RaiseInventoryChanged();
        }

        public void UseEssence(EssenceData essence)
        {
            if (!items.ContainsKey(essence) || items[essence] <= 0) return;

            items[essence]--;
            if (items[essence] <= 0) items.Remove(essence);

            double amount = essence.qiValue;
            if (playerStats != null && playerStats.isMeditating)
            {
                amount *= playerStats.meditationEssenceMultiplier;
                GameEvents.RaiseMeditationBonusApplied(playerStats.meditationEssenceMultiplier);
            }

            GameEvents.RaiseAddQi(amount);
            GameEvents.RaiseInventoryChanged();
        }

        public Dictionary<ItemData, int> GetItems()
        {
            return items;
        }

        public void LoadInventory(Dictionary<ItemData, int> loaded)
        {
            items = new Dictionary<ItemData, int>(loaded);
            GameEvents.RaiseInventoryChanged();
        }
    }
}
