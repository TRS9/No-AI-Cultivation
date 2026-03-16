using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Data;
using CultivationGame.Player;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class InventoryDisplay : MonoBehaviour
    {
        public Transform slotContainer;
        public GameObject slotPrefab;
        [HideInInspector] public PlayerInventory playerInventory;

        private readonly List<InventorySlotDisplay> _activeSlots = new();

        public void RefreshDisplay(Dictionary<ItemData, int> items)
        {
            int index = 0;
            foreach (var entry in items)
            {
                InventorySlotDisplay slot;
                if (index < _activeSlots.Count)
                {
                    slot = _activeSlots[index];
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    var go = Instantiate(slotPrefab, slotContainer);
                    slot = go.GetComponent<InventorySlotDisplay>();
                    _activeSlots.Add(slot);
                }
                slot.Setup(entry.Key, entry.Value, item =>
                {
                    if (item is EssenceData e) playerInventory.UseEssence(e);
                });
                index++;
            }

            for (int i = index; i < _activeSlots.Count; i++)
            {
                _activeSlots[i].gameObject.SetActive(false);
            }
        }
    }
}
