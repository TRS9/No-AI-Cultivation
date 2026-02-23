using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    private Dictionary<EssenceData, int> items = new Dictionary<EssenceData, int>();

    public void AddItem(EssenceData essence)
    {
        if (essence == null) return;

        if (items.ContainsKey(essence))
        {
            items[essence]++;
        }
        else
        {
            items.Add(essence, 1);
        }

        Debug.Log($"Inventar-Update: {essence.essenceName} Anzahl: {items[essence]}");
        ShowInventoryContent();
    }

    public Dictionary<EssenceData, int> GetItems()
    {
        return items;
    }
    private void ShowInventoryContent()
    {
        string content = "Im Beutel: ";
        foreach (var entry in items)
        {
            content += $"{entry.Key.essenceName} ({entry.Value}), ";
        }
        Debug.Log(content);
    }
}