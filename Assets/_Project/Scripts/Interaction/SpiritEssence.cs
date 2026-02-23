using UnityEngine;

public class SpiritEssence : MonoBehaviour, I_Interactable
{
    public EssenceData data;

    private void Start()
    {
        if (data != null)
        {
            GetComponent<Renderer>().material.color = data.essenceColor;
        }
    }

    public void Interact(GameObject user)
    {
        if (data != null)
        {
            Debug.Log($"Collected {data.essenceName}! It granted {data.qiValue} Qi.");

            PlayerStats stats = user.GetComponent<PlayerStats>();

            if (stats != null)
            {
                stats.AddQi(data.qiValue);
            }
            PlayerInventory inventory = user.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.AddItem(data);
            }
        }

        Destroy(gameObject);
    }
}