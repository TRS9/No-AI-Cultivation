using UnityEngine;
using UnityEngine.UIElements;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;
using CultivationGame.Player;

namespace CultivationGame.UI
{
    /// <summary>
    /// Controls the Build Menu panel that appears when Build Mode is toggled (B key).
    /// Lists available machines as clickable slots; clicking a slot starts ghost placement.
    /// Unaffordable slots are greyed out and show the missing resource cost.
    /// </summary>
    public class BuildMenuController : MonoBehaviour
    {
        [SerializeField] private BuildMenuDataSource buildMenuData;
        [SerializeField] private PlacementController placementController;
        [SerializeField] private PlayerInventory playerInventory;

        private VisualElement _panel;
        private ScrollView _machineGrid;
        private bool _isOpen;

        private const string PanelId = "BuildMenu";
        private const string LockedClass = "build-slot--locked";

        // ------------------------------------------------------------------ //
        //  Lifecycle
        // ------------------------------------------------------------------ //

        public void InitializeUI(VisualElement root)
        {
            _panel = root.Q<VisualElement>("BuildMenuPanel");
            _machineGrid = root.Q<ScrollView>("MachineGrid");

            // Unsubscribe first to prevent double-registration if InitializeUI is called again
            GameEvents.OnBuildModeToggled -= OnBuildModeToggled;
            GameEvents.OnPanelStateChanged -= OnPanelStateChanged;
            GameEvents.OnInventoryChanged -= OnInventoryChanged;
            GameEvents.OnBuildModeToggled += OnBuildModeToggled;
            GameEvents.OnPanelStateChanged += OnPanelStateChanged;
            GameEvents.OnInventoryChanged += OnInventoryChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnBuildModeToggled -= OnBuildModeToggled;
            GameEvents.OnPanelStateChanged -= OnPanelStateChanged;
            GameEvents.OnInventoryChanged -= OnInventoryChanged;
        }

        // ------------------------------------------------------------------ //
        //  Event handlers
        // ------------------------------------------------------------------ //

        private void OnInventoryChanged()
        {
            if (_isOpen) RebuildGrid();
        }

        private void OnBuildModeToggled(bool isBuildMode)
        {
            if (_panel == null) return;

            if (isBuildMode)
            {
                // Close other panels first (Inventory, MachineInspect, etc.)
                GameStateManager.Instance?.CloseAllPanels();

                buildMenuData?.BuildMachineList();
                RebuildGrid();
                _panel.style.display = DisplayStyle.Flex;
                _isOpen = true;
            }
            else
            {
                CloseBuildMenu();
            }
        }

        private void OnPanelStateChanged(string panelId, bool isOpen)
        {
            // If another panel opens while build menu is up, close build menu
            if (isOpen && _isOpen && panelId != PanelId)
            {
                CloseBuildMenu();
                // Also exit build mode in the camera system
                GameEvents.RaiseBuildModeToggled(false);
            }
        }

        private void CloseBuildMenu()
        {
            if (_panel != null)
                _panel.style.display = DisplayStyle.None;
            _isOpen = false;
            placementController?.CancelPlacement();
        }

        // ------------------------------------------------------------------ //
        //  UI construction
        // ------------------------------------------------------------------ //

        private void RebuildGrid()
        {
            if (_machineGrid == null || buildMenuData == null) return;
            _machineGrid.contentContainer.Clear();

            foreach (var data in buildMenuData.Machines)
            {
                var machine = data.Machine;
                bool canAfford = CanAfford(machine);

                var slot = new Button();
                slot.AddToClassList("build-slot");
                if (!canAfford)
                    slot.AddToClassList(LockedClass);

                if (data.Icon != null)
                {
                    var icon = new VisualElement();
                    icon.AddToClassList("build-slot__icon");
                    icon.style.backgroundImage = new StyleBackground(data.Icon);
                    slot.Add(icon);
                }

                var label = new Label(data.Name);
                label.AddToClassList("build-slot__label");
                slot.Add(label);

                // If unaffordable, show what's missing below the name
                if (!canAfford)
                {
                    string missing = GetMissingCostText(machine);
                    var costLabel = new Label(missing);
                    costLabel.AddToClassList("build-slot__cost");
                    slot.Add(costLabel);
                }

                // Capture for closure
                var capturedMachine = machine;
                slot.clicked += () => OnSlotClicked(capturedMachine);

                _machineGrid.contentContainer.Add(slot);
            }
        }

        private void OnSlotClicked(MachineData machine)
        {
            if (machine == null || placementController == null) return;

            if (!CanAfford(machine))
            {
                // Rebuild to refresh cost display (inventory may have changed)
                RebuildGrid();
                return;
            }

            placementController.StartPlacement(machine);
        }

        // ------------------------------------------------------------------ //
        //  Cost checking
        // ------------------------------------------------------------------ //

        private bool CanAfford(MachineData machine)
        {
            if (machine == null || machine.buildCost == null || machine.buildCost.Length == 0)
                return true;
            if (playerInventory == null) return true;

            foreach (var cost in machine.buildCost)
            {
                if (cost.item == null) continue;
                if (!playerInventory.HasItem(cost.item, cost.amount))
                    return false;
            }
            return true;
        }

        private string GetMissingCostText(MachineData machine)
        {
            if (machine.buildCost == null || playerInventory == null) return "";

            var sb = new System.Text.StringBuilder();
            var items = playerInventory.GetItems();

            foreach (var cost in machine.buildCost)
            {
                if (cost.item == null) continue;
                items.TryGetValue(cost.item, out int have);
                if (have < cost.amount)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append($"{cost.item.name} {have}/{cost.amount}");
                }
            }
            return sb.ToString();
        }
    }
}
