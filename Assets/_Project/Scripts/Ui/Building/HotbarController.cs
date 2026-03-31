using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;

namespace CultivationGame.UI
{
    public class HotbarController : MonoBehaviour
    {
        [SerializeField] private HotbarDataSource dataSource;
        [SerializeField] private PlacementController placementController;

        private VisualElement _panel;
        private VisualElement[] _slotElements = new VisualElement[10];
        private int _selectedIndex = -1;

        private static readonly string[] SlotLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        // ------------------------------------------------------------------ //
        //  Lifecycle
        // ------------------------------------------------------------------ //

        public void InitializeUI(VisualElement root)
        {
            _panel = root.Q<VisualElement>("HotbarPanel");
            if (_panel == null)
            {
                _panel = new VisualElement { name = "HotbarPanel" };
                _panel.AddToClassList("hotbar-panel");
                root.Add(_panel);
            }

            BuildSlots();

            _panel.style.display = DisplayStyle.None;

            GameEvents.OnBuildModeToggled -= OnBuildModeToggled;
            GameEvents.OnBuildModeToggled += OnBuildModeToggled;

            if (dataSource != null)
            {
                dataSource.OnSlotChanged -= OnSlotDataChanged;
                dataSource.OnSlotChanged += OnSlotDataChanged;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnBuildModeToggled -= OnBuildModeToggled;

            if (dataSource != null)
                dataSource.OnSlotChanged -= OnSlotDataChanged;
        }

        private void Update()
        {
            if (_panel == null || _panel.style.display == DisplayStyle.None) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) SelectSlot(0);
            else if (kb.digit2Key.wasPressedThisFrame) SelectSlot(1);
            else if (kb.digit3Key.wasPressedThisFrame) SelectSlot(2);
            else if (kb.digit4Key.wasPressedThisFrame) SelectSlot(3);
            else if (kb.digit5Key.wasPressedThisFrame) SelectSlot(4);
            else if (kb.digit6Key.wasPressedThisFrame) SelectSlot(5);
            else if (kb.digit7Key.wasPressedThisFrame) SelectSlot(6);
            else if (kb.digit8Key.wasPressedThisFrame) SelectSlot(7);
            else if (kb.digit9Key.wasPressedThisFrame) SelectSlot(8);
            else if (kb.digit0Key.wasPressedThisFrame) SelectSlot(9);
        }

        // ------------------------------------------------------------------ //
        //  Slot selection
        // ------------------------------------------------------------------ //

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= _slotElements.Length) return;

            // Remove previous selection highlight
            if (_selectedIndex >= 0 && _selectedIndex < _slotElements.Length)
                _slotElements[_selectedIndex].RemoveFromClassList("hotbar-slot--selected");

            _selectedIndex = index;
            _slotElements[index].AddToClassList("hotbar-slot--selected");

            if (dataSource == null) return;

            var machineData = dataSource.GetSlot(index);
            if (machineData != null && placementController != null)
                placementController.StartPlacement(machineData);
        }

        // ------------------------------------------------------------------ //
        //  Event handlers
        // ------------------------------------------------------------------ //

        private void OnBuildModeToggled(bool isBuildMode)
        {
            if (_panel == null) return;
            _panel.style.display = isBuildMode ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isBuildMode)
            {
                // Clear selection when exiting build mode
                if (_selectedIndex >= 0 && _selectedIndex < _slotElements.Length)
                    _slotElements[_selectedIndex].RemoveFromClassList("hotbar-slot--selected");
                _selectedIndex = -1;
            }
        }

        private void OnSlotDataChanged(int index)
        {
            if (index < 0 || index >= _slotElements.Length) return;
            RefreshSlot(index);
        }

        // ------------------------------------------------------------------ //
        //  UI construction
        // ------------------------------------------------------------------ //

        private void BuildSlots()
        {
            _panel.Clear();

            for (int i = 0; i < 10; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("hotbar-slot");

                var machineData = dataSource != null ? dataSource.GetSlot(i) : null;
                ApplySlotContent(slot, machineData, i);

                _slotElements[i] = slot;
                _panel.Add(slot);
            }
        }

        private void RefreshSlot(int index)
        {
            var slot = _slotElements[index];
            if (slot == null) return;

            slot.Clear();
            slot.RemoveFromClassList("hotbar-slot--filled");
            slot.RemoveFromClassList("hotbar-slot--empty");

            var machineData = dataSource != null ? dataSource.GetSlot(index) : null;
            ApplySlotContent(slot, machineData, index);

            // Preserve selection state
            if (index == _selectedIndex)
                slot.AddToClassList("hotbar-slot--selected");
        }

        private void ApplySlotContent(VisualElement slot, MachineData machineData, int index)
        {
            if (machineData != null)
            {
                slot.AddToClassList("hotbar-slot--filled");

                if (machineData.icon != null)
                {
                    var icon = new VisualElement();
                    icon.AddToClassList("hotbar-slot__icon");
                    icon.style.backgroundImage = new StyleBackground(machineData.icon);
                    slot.Add(icon);
                }
            }
            else
            {
                slot.AddToClassList("hotbar-slot--empty");
            }

            var label = new Label(SlotLabels[index]);
            label.AddToClassList("hotbar-slot__label");
            slot.Add(label);
        }
    }
}
