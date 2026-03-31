using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Player;

namespace CultivationGame.UI
{
    /// <summary>
    /// Grid-based machine catalogue that opens inside Build Mode (Tab key).
    /// Machines are dragged from the catalogue grid onto HotbarController slots.
    ///
    /// Cursor handling:
    ///   - Build mode keeps the cursor locked (CameraSystem).
    ///   - When the catalogue opens, the cursor is freed for drag interaction
    ///     and CameraSystem disables camera look via the PanelStateChanged event.
    ///   - When the catalogue closes, the cursor re-locks and camera look resumes.
    ///
    /// Does NOT use GameStateManager.OpenPanel because that disables the Player
    /// action map, which would block the B and Tab keys.
    /// </summary>
    public class MachineCatalogueController : MonoBehaviour
    {
        [SerializeField] private BuildMenuDataSource catalogueSource;
        [SerializeField] private HotbarDataSource hotbarData;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private InputActionReference catalogueToggleAction;

        private VisualElement _root;
        private VisualElement _panel;
        private ScrollView _grid;
        private bool _isOpen;
        private bool _inBuildMode;

        // Drag state
        private bool _isDragging;
        private MachineData _dragPayload;
        private VisualElement _dragGhost;
        private int _dragOverSlotIndex = -1;

        private const string PanelId = "MachineCatalogue";

        // ------------------------------------------------------------------ //
        //  Lifecycle
        // ------------------------------------------------------------------ //

        public void InitializeUI(VisualElement root)
        {
            _root = root;

            // Prefer elements defined in UXML; fall back to programmatic creation
            _panel = root.Q<VisualElement>("MachineCataloguePanel");
            if (_panel == null)
            {
                _panel = new VisualElement { name = "MachineCataloguePanel" };
                _panel.AddToClassList("panel");
                _panel.AddToClassList("catalogue-panel");
                root.Add(_panel);

                var header = new VisualElement();
                header.AddToClassList("panel-header");

                var title = new Label("MACHINE CATALOGUE");
                title.AddToClassList("panel-title");
                title.AddToClassList("catalogue-title");
                header.Add(title);

                var closeBtn = new Button { name = "CloseCatalogueBtn", text = "\u2715" };
                closeBtn.AddToClassList("close-btn");
                header.Add(closeBtn);

                _panel.Add(header);
            }

            _grid = _panel.Q<ScrollView>("CatalogueGrid");
            if (_grid == null)
            {
                _grid = new ScrollView(ScrollViewMode.Vertical) { name = "CatalogueGrid" };
                _grid.AddToClassList("catalogue-grid");
                _panel.Add(_grid);
            }

            // Close button (works whether from UXML or auto-created)
            _panel.Q<Button>("CloseCatalogueBtn")
                ?.RegisterCallback<ClickEvent>(e => Close());

            _panel.style.display = DisplayStyle.None;

            // Event subscriptions (unsubscribe first to prevent double-registration)
            GameEvents.OnBuildModeToggled -= OnBuildModeToggled;
            GameEvents.OnBuildModeToggled += OnBuildModeToggled;

            if (catalogueToggleAction != null)
            {
                catalogueToggleAction.action.performed -= OnToggleInput;
                catalogueToggleAction.action.performed += OnToggleInput;
            }

            // Root-level pointer tracking for drag move and drop
            _root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _root.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _root.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        private void OnDisable()
        {
            GameEvents.OnBuildModeToggled -= OnBuildModeToggled;

            if (catalogueToggleAction != null)
                catalogueToggleAction.action.performed -= OnToggleInput;

            if (_root != null)
            {
                _root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                _root.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            }
        }

        // ------------------------------------------------------------------ //
        //  Toggle
        // ------------------------------------------------------------------ //

        private void OnToggleInput(InputAction.CallbackContext ctx)
        {
            if (!_inBuildMode) return;
            if (_isOpen) Close(); else Open();
        }

        private void OnBuildModeToggled(bool isBuildMode)
        {
            _inBuildMode = isBuildMode;
            if (!isBuildMode && _isOpen)
                Close();
        }

        private void Open()
        {
            if (_isOpen) return;
            _isOpen = true;

            if (catalogueSource != null && catalogueSource.Machines.Count == 0)
                catalogueSource.BuildMachineList();

            RebuildGrid();
            _panel.style.display = DisplayStyle.Flex;

            // Free cursor so the player can click and drag
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;

            // Tell CameraSystem to pause camera look input
            GameEvents.RaisePanelStateChanged(PanelId, true);
        }

        private void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

            CancelDrag();
            _panel.style.display = DisplayStyle.None;

            // Re-lock cursor (build mode keeps the cursor locked)
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;

            // Tell CameraSystem to resume camera look input
            GameEvents.RaisePanelStateChanged(PanelId, false);
        }

        // ------------------------------------------------------------------ //
        //  Grid construction (mirrors InventoryController shelf-slot pattern)
        // ------------------------------------------------------------------ //

        private void RebuildGrid()
        {
            if (_grid == null || catalogueSource == null) return;
            _grid.contentContainer.Clear();

            foreach (var data in catalogueSource.Machines)
            {
                var machine = data.Machine;
                if (machine == null) continue;

                bool canAfford = CanAfford(machine);

                var slot = new VisualElement();
                slot.AddToClassList("catalogue-slot");
                if (!canAfford)
                    slot.AddToClassList("catalogue-slot--locked");

                // Machine icon
                if (data.Icon != null)
                {
                    var icon = new VisualElement();
                    icon.AddToClassList("catalogue-slot__icon");
                    icon.style.backgroundImage = new StyleBackground(data.Icon);
                    slot.Add(icon);
                }

                // Jade platform (decoration, same as inventory)
                var platform = new VisualElement();
                platform.AddToClassList("catalogue-slot__platform");
                slot.Add(platform);

                // Machine name
                var label = new Label(data.Name);
                label.AddToClassList("catalogue-slot__label");
                slot.Add(label);

                // Drag start
                var captured = machine;
                var capturedIcon = data.Icon;
                slot.RegisterCallback<PointerDownEvent>(evt =>
                    OnSlotPointerDown(evt, captured, capturedIcon));

                _grid.contentContainer.Add(slot);
            }
        }

        // ------------------------------------------------------------------ //
        //  Drag & Drop
        // ------------------------------------------------------------------ //

        private void OnSlotPointerDown(PointerDownEvent evt, MachineData machine, Sprite icon)
        {
            if (evt.button != 0) return;

            _isDragging = true;
            _dragPayload = machine;

            // Floating ghost that follows the pointer
            _dragGhost = new VisualElement();
            _dragGhost.AddToClassList("catalogue-drag-ghost");
            _dragGhost.style.position = Position.Absolute;
            _dragGhost.pickingMode = PickingMode.Ignore;

            if (icon != null)
                _dragGhost.style.backgroundImage = new StyleBackground(icon);

            PositionGhost(evt.position);
            _root.Add(_dragGhost);

            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || _dragGhost == null) return;
            PositionGhost(evt.position);
            UpdateDragOverHighlight(evt.position);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging) return;

            int slotIndex = FindHotbarSlotAtPosition(evt.position);
            if (slotIndex >= 0 && hotbarData != null && _dragPayload != null)
                hotbarData.SetSlot(slotIndex, _dragPayload);

            ClearDragOverHighlight();
            CancelDrag();
        }

        private void CancelDrag()
        {
            if (_dragGhost != null)
            {
                _dragGhost.parent?.Remove(_dragGhost);
                _dragGhost = null;
            }
            _isDragging = false;
            _dragPayload = null;
        }

        private void PositionGhost(Vector2 pos)
        {
            if (_dragGhost == null) return;
            _dragGhost.style.left = pos.x - 24;
            _dragGhost.style.top = pos.y - 24;
        }

        // ------------------------------------------------------------------ //
        //  Drag-over highlight on hotbar slots
        // ------------------------------------------------------------------ //

        private void UpdateDragOverHighlight(Vector2 pos)
        {
            int idx = FindHotbarSlotAtPosition(pos);
            if (idx == _dragOverSlotIndex) return;

            ClearDragOverHighlight();
            _dragOverSlotIndex = idx;

            if (idx >= 0)
            {
                var slots = _root.Query(className: "hotbar-slot").ToList();
                if (idx < slots.Count)
                    slots[idx].AddToClassList("hotbar-slot--drag-over");
            }
        }

        private void ClearDragOverHighlight()
        {
            if (_dragOverSlotIndex >= 0)
            {
                var slots = _root.Query(className: "hotbar-slot").ToList();
                if (_dragOverSlotIndex < slots.Count)
                    slots[_dragOverSlotIndex].RemoveFromClassList("hotbar-slot--drag-over");
            }
            _dragOverSlotIndex = -1;
        }

        private int FindHotbarSlotAtPosition(Vector2 pos)
        {
            if (_root == null) return -1;

            var slots = _root.Query(className: "hotbar-slot").ToList();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].worldBound.Contains(pos))
                    return i;
            }
            return -1;
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
    }
}
