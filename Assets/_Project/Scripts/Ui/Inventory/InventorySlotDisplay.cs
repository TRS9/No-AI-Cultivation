using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Data;

namespace CultivationGame.UI
{
    [RequireComponent(typeof(Button))]
    public class InventorySlotDisplay : MonoBehaviour
    {
        public Image iconImage;
        public TextMeshProUGUI amountText;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();
        }

        public void Setup(EssenceData data, int amount, Action<EssenceData> onUse)
        {
            iconImage.sprite = data.icon;
            iconImage.color = data.essenceColor;

            if (amountText != null)
                amountText.text = amount > 1 ? amount.ToString() : "";

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onUse?.Invoke(data));
        }
    }
}
