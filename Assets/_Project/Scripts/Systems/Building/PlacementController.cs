using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Player;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Manages the ghost-build placement flow:
    ///   1. Player selects a machine from the Build Menu → StartPlacement()
    ///   2. A semi-transparent ghost follows the cursor, snapped to the grid
    ///   3. Left-click confirms placement; right-click / Escape cancels
    ///   4. R key rotates the ghost in 90° steps
    /// Also handles machine removal when not in placement mode.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildGrid buildGrid;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private Camera buildCamera;

        [Header("Ghost Settings")]
        [SerializeField] private Material ghostValidMaterial;
        [SerializeField] private Material ghostInvalidMaterial;

        [Header("Input")]
        [SerializeField] private InputActionReference placeAction;
        [SerializeField] private InputActionReference cancelAction;
        [SerializeField] private InputActionReference rotateAction;
        [SerializeField] private InputActionReference removeAction;

        [Header("Removal")]
        [SerializeField] private LayerMask machineLayer;

        private MachineData _selectedMachine;
        private GameObject _ghostInstance;
        private Renderer[] _ghostRenderers;
        private bool _isPlacing;
        private bool _canPlace;
        private int _rotation; // 0, 1, 2, 3 → 0°, 90°, 180°, 270°

        public bool IsPlacing => _isPlacing;

        // ------------------------------------------------------------------ //
        //  Input wiring
        // ------------------------------------------------------------------ //

        private void OnEnable()
        {
            if (placeAction != null)
                placeAction.action.performed += OnPlace;
            if (cancelAction != null)
                cancelAction.action.performed += OnCancel;
            if (rotateAction != null)
                rotateAction.action.performed += OnRotate;
            if (removeAction != null)
                removeAction.action.performed += OnRemove;
        }

        private void OnDisable()
        {
            if (placeAction != null)
                placeAction.action.performed -= OnPlace;
            if (cancelAction != null)
                cancelAction.action.performed -= OnCancel;
            if (rotateAction != null)
                rotateAction.action.performed -= OnRotate;
            if (removeAction != null)
                removeAction.action.performed -= OnRemove;

            // Clean up ghost if we get disabled mid-placement
            if (_isPlacing) CancelPlacement();
        }

        // ------------------------------------------------------------------ //
        //  Update loop – move the ghost every frame
        // ------------------------------------------------------------------ //

        private void Update()
        {
            if (!_isPlacing || _ghostInstance == null) return;
            UpdateGhostPosition();
        }

        // ------------------------------------------------------------------ //
        //  Public API
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Called from the Build Menu UI when a machine is selected.
        /// Spawns the ghost preview and enters placement mode.
        /// </summary>
        public void StartPlacement(MachineData machine)
        {
            if (machine == null) return;

            // Cancel any existing placement first
            if (_isPlacing) CancelPlacement();

            _selectedMachine = machine;
            _rotation = 0;
            _isPlacing = true;

            // Instantiate ghost preview (prefer dedicated ghost prefab, fall back to real prefab)
            GameObject ghostPrefab = machine.ghostPrefab != null ? machine.ghostPrefab : machine.prefab;
            _ghostInstance = Instantiate(ghostPrefab);
            _ghostInstance.name = $"Ghost_{machine.machineName}";

            // Disable all colliders on the ghost so it doesn't interfere with raycasts
            foreach (var col in _ghostInstance.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Disable all MonoBehaviours on the ghost so it doesn't run game logic
            foreach (var mb in _ghostInstance.GetComponentsInChildren<MonoBehaviour>())
                mb.enabled = false;

            // Cache renderers for material swapping
            _ghostRenderers = _ghostInstance.GetComponentsInChildren<Renderer>();
            SetGhostMaterial(ghostValidMaterial);

            GameDataEvents.RaiseBuildModeGhostStarted(_selectedMachine);
        }

        /// <summary>
        /// Destroys the ghost and exits placement mode.
        /// </summary>
        public void CancelPlacement()
        {
            if (_ghostInstance != null)
                Destroy(_ghostInstance);

            _ghostInstance = null;
            _ghostRenderers = null;
            _selectedMachine = null;
            _isPlacing = false;
            _rotation = 0;

            GameDataEvents.RaiseBuildModeGhostCancelled();
        }

        // ------------------------------------------------------------------ //
        //  Ghost position & validity
        // ------------------------------------------------------------------ //

        private void UpdateGhostPosition()
        {
            Camera cam = buildCamera != null ? buildCamera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, terrainLayer))
            {
                _ghostInstance.SetActive(false);
                return;
            }

            _ghostInstance.SetActive(true);

            Vector3 snapped = buildGrid.SnapToGrid(hit.point);
            _ghostInstance.transform.position = snapped;
            _ghostInstance.transform.rotation = Quaternion.Euler(0f, _rotation * 90f, 0f);

            // Check validity
            bool hasResources = HasBuildResources(_selectedMachine);
            bool gridFree = buildGrid.CanPlace(snapped, _selectedMachine.gridSize, _rotation);
            _canPlace = hasResources && gridFree;

            SetGhostMaterial(_canPlace ? ghostValidMaterial : ghostInvalidMaterial);
        }

        // ------------------------------------------------------------------ //
        //  Input callbacks
        // ------------------------------------------------------------------ //

        private void OnPlace(InputAction.CallbackContext ctx)
        {
            if (!_isPlacing || !_canPlace || _selectedMachine == null) return;
            if (_ghostInstance == null || !_ghostInstance.activeSelf) return;

            Vector3 position = _ghostInstance.transform.position;
            Quaternion rotation = _ghostInstance.transform.rotation;

            // Deduct build cost from inventory
            DeductBuildResources(_selectedMachine);

            // Instantiate the real machine prefab
            GameObject placed = Instantiate(_selectedMachine.prefab, position, rotation);
            placed.name = _selectedMachine.machineName;

            // Wire machine data via IMachineConnectable if available
            var connectable = placed.GetComponent<IMachineConnectable>();
            if (connectable != null)
            {
                // All IMachineConnectable types have SetMachineData
                if (connectable is BaseMachine bm)
                    bm.SetMachineData(_selectedMachine);
                else if (connectable is ResourceExtractor ext)
                    ext.SetMachineData(_selectedMachine);
                else if (connectable is StorageContainer sc)
                    sc.SetMachineData(_selectedMachine);
            }

            // Mark grid cells as occupied
            buildGrid.OccupyCells(position, _selectedMachine.gridSize, _rotation);

            // Notify the rest of the game
            GameDataEvents.RaiseMachinePlaced(_selectedMachine, position, rotation);

            // Clean up ghost and exit placement mode
            CancelPlacement();
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (_isPlacing) CancelPlacement();
        }

        private void OnRotate(InputAction.CallbackContext ctx)
        {
            if (!_isPlacing) return;
            _rotation = (_rotation + 1) % 4;
        }

        /// <summary>
        /// Removes a placed machine under the cursor (when not in placement mode).
        /// Frees the grid cells and destroys the machine GameObject.
        /// </summary>
        private void OnRemove(InputAction.CallbackContext ctx)
        {
            if (_isPlacing) return;

            Camera cam = buildCamera != null ? buildCamera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, machineLayer)) return;

            // Try to find a machine component on the hit object
            var connectable = hit.collider.GetComponentInParent<IMachineConnectable>();
            if (connectable == null) return;

            var mb = connectable as MonoBehaviour;
            if (mb == null) return;

            MachineData data = connectable.MachineData;
            if (data == null) return;

            Vector3 position = mb.transform.position;

            // Free the grid cells
            // Determine rotation from the object's Y euler angle
            int rot = Mathf.RoundToInt(mb.transform.eulerAngles.y / 90f) % 4;
            buildGrid.FreeCells(position, data.gridSize, rot);

            // Notify the rest of the game
            GameDataEvents.RaiseMachineRemoved(data, position);

            // Destroy the machine
            Destroy(mb.gameObject);
        }

        // ------------------------------------------------------------------ //
        //  Helpers
        // ------------------------------------------------------------------ //

        private void SetGhostMaterial(Material mat)
        {
            if (_ghostRenderers == null || mat == null) return;
            foreach (var r in _ghostRenderers)
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = mat;
                r.materials = mats;
            }
        }

        private bool HasBuildResources(MachineData machine)
        {
            if (machine.buildCost == null || machine.buildCost.Length == 0) return true;
            if (playerInventory == null) return true;

            var items = playerInventory.GetItems();
            foreach (var cost in machine.buildCost)
            {
                if (cost.item == null) continue;
                if (!items.TryGetValue(cost.item, out int count) || count < cost.amount)
                    return false;
            }
            return true;
        }

        private void DeductBuildResources(MachineData machine)
        {
            if (machine.buildCost == null || playerInventory == null) return;

            var items = playerInventory.GetItems();
            foreach (var cost in machine.buildCost)
            {
                if (cost.item == null) continue;
                if (items.ContainsKey(cost.item))
                {
                    items[cost.item] -= cost.amount;
                    if (items[cost.item] <= 0) items.Remove(cost.item);
                }
            }
            GameEvents.RaiseInventoryChanged();
        }
    }
}
