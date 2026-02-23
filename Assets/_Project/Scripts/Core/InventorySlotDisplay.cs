using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotDisplay : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI amountText;

    public void Setup(EssenceData data, int amount)
    {
        iconImage.sprite = data.icon;
        iconImage.color = data.essenceColor;

        if (amountText != null)
        {
            amountText.text = amount > 1 ? amount.ToString() : "";
        }
    }
}