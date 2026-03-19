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
        private ScrollView _grid;
        private VisualElement _detailPanel;
        private Label _detailName;
        private Label _detailType;
        private VisualElement _detailGradeRow;
        private Label _detailGrade;
        private Label _detailQi;
        private Label _detailDesc;
        private Button _detailUseBtn;

        private int _selectedIndex = -1;
        private InventorySlotData _selectedItem;

        public void InitializeUI(VisualElement root)
        {
            _panel = root.Q<VisualElement>("InventoryPanel");
            _grid = root.Q<ScrollView>("InventoryGrid");
            _detailPanel = root.Q<VisualElement>("ItemDetailPanel");
            _detailName = root.Q<Label>("DetailItemName");
            _detailType = root.Q<Label>("DetailItemType");
            _detailGradeRow = root.Q<VisualElement>("DetailGradeRow");
            _detailGrade = root.Q<Label>("DetailItemGrade");
            _detailQi = root.Q<Label>("DetailItemQi");
            _detailDesc = root.Q<Label>("DetailItemDesc");
            _detailUseBtn = root.Q<Button>("DetailUseBtn");

            _panel?.Q<Button>("CloseInventoryBtn")
                ?.RegisterCallback<ClickEvent>(e => RequestClose());

            _detailUseBtn?.RegisterCallback<ClickEvent>(e => UseSelectedItem());

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
                _selectedIndex = -1;
                HideDetail();
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
                    RebuildGrid();
                    break;
            }
        }

        private void RebuildGrid()
        {
            if (_grid == null) return;

            _grid.contentContainer.Clear();

            for (int i = 0; i < inventoryData.Items.Count; i++)
            {
                var data = inventoryData.Items[i];
                int index = i;

                var slot = new VisualElement();
                slot.AddToClassList("shelf-slot");

                // Quantity badge
                if (data.Quantity > 1)
                {
                    var qty = new Label { text = $"x{data.Quantity}" };
                    qty.AddToClassList("shelf-slot__qty");
                    slot.Add(qty);
                }

                // Item icon
                var icon = new VisualElement();
                icon.AddToClassList("shelf-slot__icon");
                if (data.Icon != null)
                    icon.style.backgroundImage = new StyleBackground(data.Icon);
                slot.Add(icon);

                // Jade platform
                var platform = new VisualElement();
                platform.AddToClassList("shelf-slot__platform");
                slot.Add(platform);

                // Selection
                slot.RegisterCallback<ClickEvent>(e => SelectSlot(index));

                if (index == _selectedIndex)
                    slot.AddToClassList("shelf-slot--selected");

                _grid.contentContainer.Add(slot);
            }
        }

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= inventoryData.Items.Count) return;

            _selectedIndex = index;
            _selectedItem = inventoryData.Items[index];

            // Update selection visuals
            var slots = _grid.contentContainer.Children();
            int i = 0;
            foreach (var slot in slots)
            {
                slot.EnableInClassList("shelf-slot--selected", i == index);
                i++;
            }

            ShowDetail(_selectedItem);
        }

        private void ShowDetail(InventorySlotData data)
        {
            if (_detailPanel == null) return;

            _detailPanel.style.display = DisplayStyle.Flex;

            // Name: prefer type-specific name, fall back to generic
            string displayName = data.Name;
            if (data.Item is PillData pill && !string.IsNullOrEmpty(pill.pillName))
                displayName = pill.pillName;

            if (_detailName != null) _detailName.text = displayName;
            if (_detailType != null) _detailType.text = data.ItemType.ToString();
            if (_detailQi != null) _detailQi.text = data.Item != null ? data.Item.qiValue.ToString() : "0";

            // Description
            if (_detailDesc != null)
                _detailDesc.text = data.Item != null ? data.Item.description : "";

            // Grade row: only for pills
            bool isPill = data.Item is PillData;
            if (_detailGradeRow != null)
                _detailGradeRow.style.display = isPill ? DisplayStyle.Flex : DisplayStyle.None;
            if (isPill && _detailGrade != null)
                _detailGrade.text = ((PillData)data.Item).grade.ToString();

            // Use button: only for Essence/Pill
            bool usable = data.ItemType == ItemType.Essence || data.ItemType == ItemType.Pill;
            if (_detailUseBtn != null)
                _detailUseBtn.style.display = usable ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HideDetail()
        {
            if (_detailPanel != null)
                _detailPanel.style.display = DisplayStyle.None;
        }

        private void UseSelectedItem()
        {
            if (playerInventory == null || _selectedItem.Item == null) return;

            if (_selectedItem.Item is PillData pill)
                playerInventory.UsePill(pill);
            else if (_selectedItem.Item is EssenceData essence)
                playerInventory.UseEssence(essence);
        }
    }
}
