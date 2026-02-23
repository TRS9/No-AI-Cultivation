using UnityEngine;
using System.Collections.Generic;

public class InventoryDisplay : MonoBehaviour
{
    public Transform slotContainer; 
    public GameObject slotPrefab;

    public void RefreshDisplay(Dictionary<EssenceData, int> items)
    {

        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var entry in items)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            InventorySlotDisplay slotScript = newSlot.GetComponent<InventorySlotDisplay>();

            if (slotScript != null)
            {
                slotScript.Setup(entry.Key, entry.Value);
            }
        }
    }
}