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
    ///   2. A semi-transparent ghost follows the screen-centre crosshair, snapped to the grid
    ///   3. Left-click confirms placement; right-click / Escape cancels
    ///   4. R key rotates the ghost in 90° steps
    /// Also handles machine removal when not in placement mode.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] [Tooltip("Reference to the build grid that snaps machine positions and tracks occupied cells.")] private BuildGrid buildGrid;
        [SerializeField] [Tooltip("Player inventory used to check and deduct machine build costs.")] private PlayerInventory playerInventory;
        [SerializeField] [Tooltip("Layer mask for terrain raycasts to determine valid placement positions.")] private LayerMask terrainLayer;
        [SerializeField] [Tooltip("Camera used for placement raycasts; falls back to Camera.main if unassigned.")] private Camera buildCamera;

        [Header("Ghost Settings")]
        [SerializeField] [Tooltip("Material applied to the placement ghost when the current position is valid.")] private Material ghostValidMaterial;
        [SerializeField] [Tooltip("Material applied to the placement ghost when the current position is invalid.")] private Material ghostInvalidMaterial;

        [Header("Input")]
        [SerializeField] [Tooltip("Input action that confirms machine placement (left mouse button).")] private InputActionReference placeAction;
        [SerializeField] [Tooltip("Input action that cancels the current placement (Escape).")] private InputActionReference cancelAction;
        [SerializeField] [Tooltip("Input action that rotates the ghost 90° clockwise (R key).")] private InputActionReference rotateAction;
        [SerializeField] [Tooltip("Input action that removes the machine under the crosshair.")] private InputActionReference removeAction;

        [Header("Removal")]
        [SerializeField] [Tooltip("Layer mask used to detect placed machines for removal raycasts.")] private LayerMask machineLayer;

        private MachineData _selectedMachine;
        private GameObject _ghostInstance;
        private Renderer[] _ghostRenderers;
        private Material[][] _cachedMaterialArrays;
        private bool _isPlacing;
        private bool _canPlace;
        private int _rotation; // 0, 1, 2, 3 → 0°, 90°, 180°, 270°

        public bool IsPlacing => _isPlacing;

        private static Vector3 ScreenCenter => new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

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
            if (machine.prefab == null && machine.ghostPrefab == null)
            {
                Debug.LogWarning($"[PlacementController] '{machine.machineName}' has no prefab assigned — cannot place.");
                return;
            }

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

            // Pre-allocate material arrays to avoid per-frame heap allocations
            _cachedMaterialArrays = new Material[_ghostRenderers.Length][];
            for (int i = 0; i < _ghostRenderers.Length; i++)
                _cachedMaterialArrays[i] = new Material[_ghostRenderers[i].sharedMaterials.Length];

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
            _cachedMaterialArrays = null;
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

            // Raycast from screen centre (the crosshair) instead of the mouse cursor
            Ray ray = cam.ScreenPointToRay(ScreenCenter);
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

            // Wire machine data to the placed component
            if (placed.GetComponent<BaseMachine>() is BaseMachine bm)
                bm.SetMachineData(_selectedMachine);
            else if (placed.GetComponent<ResourceExtractor>() is ResourceExtractor ext)
                ext.SetMachineData(_selectedMachine);
            else if (placed.GetComponent<StorageContainer>() is StorageContainer sc)
                sc.SetMachineData(_selectedMachine);
            else if (placed.GetComponent<QiConduit>() is QiConduit conduit)
                conduit.SetMachineData(_selectedMachine);
            else if (placed.GetComponent<Splitter>() is Splitter splitter)
                splitter.SetMachineData(_selectedMachine);
            else if (placed.GetComponent<Merger>() is Merger merger)
                merger.SetMachineData(_selectedMachine);

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
        /// Removes a placed machine under the crosshair (when not in placement mode).
        /// Frees the grid cells and destroys the machine GameObject.
        /// </summary>
        private void OnRemove(InputAction.CallbackContext ctx)
        {
            if (_isPlacing) return;

            Camera cam = buildCamera != null ? buildCamera : Camera.main;
            if (cam == null) return;

            // Raycast from screen centre (the crosshair)
            Ray ray = cam.ScreenPointToRay(ScreenCenter);
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
            for (int i = 0; i < _ghostRenderers.Length; i++)
            {
                var mats = _cachedMaterialArrays[i];
                for (int j = 0; j < mats.Length; j++)
                    mats[j] = mat;
                _ghostRenderers[i].materials = mats;
            }
        }

        private bool HasBuildResources(MachineData machine)
        {
            if (machine.buildCost == null || machine.buildCost.Length == 0) return true;
            if (playerInventory == null) return true;

            foreach (var cost in machine.buildCost)
            {
                if (cost.item == null) continue;
                if (!playerInventory.HasItem(cost.item, cost.amount))
                    return false;
            }
            return true;
        }

        private void DeductBuildResources(MachineData machine)
        {
            if (machine.buildCost == null || playerInventory == null) return;

            foreach (var cost in machine.buildCost)
            {
                if (cost.item == null) continue;
                playerInventory.RemoveItem(cost.item, cost.amount);
            }
        }
    }
}
