using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Player
{
    public class PlayerInventory : MonoBehaviour, IInventory
    {
        public PlayerStats playerStats;

        private Dictionary<EssenceData, int> items = new Dictionary<EssenceData, int>();

        public void AddItem(ScriptableObject item)
        {
            if (item is not EssenceData essence || essence == null) return;

            if (items.ContainsKey(essence))
            {
                items[essence]++;
            }
            else
            {
                items.Add(essence, 1);
            }

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

        public Dictionary<EssenceData, int> GetItems()
        {
            return items;
        }

        public void LoadInventory(Dictionary<EssenceData, int> loaded)
        {
            items = new Dictionary<EssenceData, int>(loaded);
            GameEvents.RaiseInventoryChanged();
        }
    }
}
