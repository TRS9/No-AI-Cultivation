using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Player;

namespace CultivationGame.UI
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private InventoryDataSource inventoryData;
        [SerializeField] private PlayerInventory playerInventory;

        private VisualElement _panel;
        private ListView _list;

        public void InitializeUI(VisualElement root)
        {
            _panel = root.Q<VisualElement>("InventoryPanel");
            _list = root.Q<ListView>("InventoryList");

            _panel?.Q<Button>("CloseInventoryBtn")
                ?.RegisterCallback<ClickEvent>(e => RequestClose());

            SetupListView();

            if (inventoryData != null)
            {
                inventoryData.PlayerInventory = playerInventory;
                inventoryData.ResetState();
                inventoryData.Subscribe();
                inventoryData.PropertyChanged += OnPropertyChanged;
            }

            GameEvents.OnPanelStateChanged += OnPanelStateChanged;
        }

        private void OnDisable()
        {
            if (inventoryData != null)
            {
                inventoryData.Unsubscribe();
                inventoryData.PropertyChanged -= OnPropertyChanged;
            }
            GameEvents.OnPanelStateChanged -= OnPanelStateChanged;
        }

        private void OnPanelStateChanged(string panelId, bool isOpen)
        {
            if (panelId != "Inventory") return;

            if (isOpen)
            {
                inventoryData?.RebuildItems();
                if (_panel != null) _panel.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (_panel != null) _panel.style.display = DisplayStyle.None;
            }
        }

        private void RequestClose()
        {
            GameStateManager.Instance?.ClosePanel("Inventory");
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(InventoryDataSource.Items):
                    if (_list != null)
                    {
                        _list.itemsSource = inventoryData.Items;
                        _list.Rebuild();
                    }
                    break;
            }
        }

        private void SetupListView()
        {
            if (_list == null) return;

            _list.makeItem = () =>
            {
                var slot = new VisualElement();
                slot.AddToClassList("inventory-slot");

                var icon = new VisualElement();
                icon.AddToClassList("inventory-slot__icon");
                slot.Add(icon);

                var nameLabel = new Label();
                nameLabel.AddToClassList("inventory-slot__name");
                slot.Add(nameLabel);

                var qty = new Label();
                qty.AddToClassList("inventory-slot__quantity");
                slot.Add(qty);

                var useBtn = new Button { text = "Use" };
                useBtn.AddToClassList("inventory-slot__use-btn");
                slot.Add(useBtn);

                return slot;
            };

            _list.bindItem = (element, index) =>
            {
                if (index >= inventoryData.Items.Count) return;
                var data = inventoryData.Items[index];

                var icon = element.Q<VisualElement>(className: "inventory-slot__icon");
                if (icon != null && data.Icon != null)
                    icon.style.backgroundImage = new StyleBackground(data.Icon);

                var nameLabel = element.Q<Label>(className: "inventory-slot__name");
                if (nameLabel != null) nameLabel.text = data.Name;

                var qty = element.Q<Label>(className: "inventory-slot__quantity");
                if (qty != null) qty.text = data.Quantity > 1 ? $"x{data.Quantity}" : "";

                var useBtn = element.Q<Button>(className: "inventory-slot__use-btn");
                if (useBtn != null)
                {
                    bool usable = data.ItemType == ItemType.Essence || data.ItemType == ItemType.Pill;
                    useBtn.style.display = usable ? DisplayStyle.Flex : DisplayStyle.None;
                    useBtn.clickable = new Clickable(() => UseItem(data));
                }
            };

            _list.itemsSource = inventoryData?.Items;
        }

        private void UseItem(InventorySlotData slotData)
        {
            if (playerInventory == null || slotData.Item == null) return;

            if (slotData.Item is PillData pill)
                playerInventory.UsePill(pill);
            else if (slotData.Item is EssenceData essence)
                playerInventory.UseEssence(essence);
        }
    }
}
