using UnityEngine;
using UnityEngine.UIElements;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;

namespace CultivationGame.UI
{
    /// <summary>
    /// Controls the Build Menu panel that appears when Build Mode is toggled (Shift key).
    /// Lists available machines as clickable slots; clicking a slot starts ghost placement.
    /// Works in any camera perspective (3rd person or Spirit Sense).
    /// </summary>
    public class BuildMenuController : MonoBehaviour
    {
        [SerializeField] private BuildMenuDataSource buildMenuData;
        [SerializeField] private PlacementController placementController;

        private VisualElement _panel;
        private ScrollView _machineGrid;
        private bool _inSpiritSense;

        private const string ThirdPersonClass = "build-menu--third-person";

        // ------------------------------------------------------------------ //
        //  Lifecycle
        // ------------------------------------------------------------------ //

        public void InitializeUI(VisualElement root)
        {
            _panel = root.Q<VisualElement>("BuildMenuPanel");
            _machineGrid = root.Q<ScrollView>("MachineGrid");

            GameEvents.OnBuildModeToggled += OnBuildModeToggled;
            GameEvents.OnMeditationToggled += OnMeditationToggled;
        }

        private void OnDisable()
        {
            GameEvents.OnBuildModeToggled -= OnBuildModeToggled;
            GameEvents.OnMeditationToggled -= OnMeditationToggled;
        }

        // ------------------------------------------------------------------ //
        //  Event handlers
        // ------------------------------------------------------------------ //

        private void OnMeditationToggled(bool isMeditating)
        {
            _inSpiritSense = isMeditating;
        }

        private void OnBuildModeToggled(bool isBuildMode)
        {
            if (_panel == null) return;

            if (isBuildMode)
            {
                buildMenuData?.BuildMachineList();
                RebuildGrid();
                _panel.style.display = DisplayStyle.Flex;

                // Ensure clean state before applying layout class
                _panel.RemoveFromClassList(ThirdPersonClass);

                // Apply compact layout when in 3rd-person (not Spirit Sense)
                if (!_inSpiritSense)
                    _panel.AddToClassList(ThirdPersonClass);
            }
            else
            {
                _panel.style.display = DisplayStyle.None;
                _panel.RemoveFromClassList(ThirdPersonClass);
                placementController?.CancelPlacement();
            }
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
                var slot = new Button();
                slot.AddToClassList("build-slot");

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

                // Capture for closure
                var machine = data.Machine;
                slot.clicked += () => placementController?.StartPlacement(machine);

                _machineGrid.contentContainer.Add(slot);
            }
        }
    }
}
