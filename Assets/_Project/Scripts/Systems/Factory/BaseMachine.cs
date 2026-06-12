using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Core machine component. Handles input/output inventories, recipe selection,
    /// and timer-based processing. Attach to any placed machine prefab.
    /// Requires Qi from the QiNetwork to operate (IsPowered must be true).
    /// Idle machines only re-check their recipe when something relevant changed
    /// (inventory contents, recipe, power) instead of every frame.
    /// </summary>
    public class BaseMachine : MonoBehaviour, IInteractable, IMachineConnectable
    {
        [Header("Machine Configuration")]
        [SerializeField] [Tooltip("ScriptableObject containing the machine's name, icon, type, and processing speed.")] private MachineData machineData;

        [Header("Recipe")]
        [SerializeField] [Tooltip("Recipe currently assigned to this machine.")] private RecipeData currentRecipe;

        [Header("Inventory Capacities")]
        [SerializeField] [Tooltip("Maximum item count the input inventory can hold.")] private int inputCapacity = 50;
        [SerializeField] [Tooltip("Maximum item count the output inventory can hold.")] private int outputCapacity = 50;

        private MachineInventory _inputInventory;
        private MachineInventory _outputInventory;
        private float _processingTimer;
        private float _processingDuration;
        private bool _isProcessing;
        private bool _isStalled;
        private bool _isPowered;
        private bool _recheckRecipe = true;

        // --- Public API ---
        public MachineData MachineData => machineData;
        public RecipeData CurrentRecipe => currentRecipe;
        public MachineInventory InputInventory => _inputInventory;
        public MachineInventory OutputInventory => _outputInventory;
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// Set by QiNetwork. Machine only processes when powered.
        /// A rising edge triggers a recipe re-check so idle machines wake up.
        /// </summary>
        public bool IsPowered
        {
            get => _isPowered;
            set
            {
                if (value && !_isPowered) _recheckRecipe = true;
                _isPowered = value;
            }
        }

        public float ProcessingProgress => _processingDuration > 0f
            ? Mathf.Clamp01(_processingTimer / _processingDuration) : 0f;

        public float ProcessingTimer => _processingTimer;
        public float ProcessingDuration => _processingDuration;

        private void Awake()
        {
            _inputInventory = new MachineInventory(inputCapacity);
            _outputInventory = new MachineInventory(outputCapacity);

            // Any inventory change may unblock processing (inputs arrived, output drained).
            _inputInventory.OnChanged += MarkRecipeDirty;
            _outputInventory.OnChanged += MarkRecipeDirty;
        }

        private void Start()
        {
            QiNetwork.GetOrCreate().RegisterMachine(this);
        }

        private void OnDestroy()
        {
            if (QiNetwork.Instance != null)
                QiNetwork.Instance.UnregisterMachine(this);
        }

        private void Update()
        {
            if (_isProcessing)
            {
                if (!IsPowered)
                {
                    if (!_isStalled)
                    {
                        _isStalled = true;
                        GameDataEvents.RaiseMachineStalled(this);
                    }
                    return;
                }

                _isStalled = false;
                _processingTimer += Time.deltaTime;
                if (_processingTimer >= _processingDuration)
                {
                    CompleteProcessing();
                }
            }
            else if (_recheckRecipe)
            {
                _recheckRecipe = false;
                TryStartProcessing();
            }
        }

        private void MarkRecipeDirty() => _recheckRecipe = true;

        public void SetMachineData(MachineData data)
        {
            machineData = data;
            _recheckRecipe = true;
        }

        public void SetRecipe(RecipeData recipe)
        {
            currentRecipe = recipe;
            _recheckRecipe = true;
        }

        public void Interact(GameObject user)
        {
            GameDataEvents.RaiseMachineInteracted(this);
        }

        /// <summary>
        /// Convenience method: add an item to the input inventory.
        /// </summary>
        public int AddInput(ItemData item, int amount = 1)
        {
            return _inputInventory.TryAdd(item, amount);
        }

        /// <summary>
        /// Convenience method: remove an item from the output inventory.
        /// </summary>
        public int RemoveOutput(ItemData item, int amount = 1)
        {
            return _outputInventory.TryRemove(item, amount);
        }

        // --- Processing Logic ---

        private void TryStartProcessing()
        {
            if (currentRecipe == null || machineData == null) return;
            if (!IsPowered) return;

            // Check all inputs are available
            foreach (var input in currentRecipe.inputs)
            {
                if (input.item == null) continue;
                if (!_inputInventory.HasItem(input.item, input.amount)) return;
            }

            // Check output has space for all outputs
            int totalOutput = 0;
            foreach (var output in currentRecipe.outputs)
                totalOutput += output.amount;
            if (!_outputInventory.HasSpace(totalOutput)) return;

            // Consume inputs
            foreach (var input in currentRecipe.inputs)
            {
                if (input.item == null) continue;
                _inputInventory.TryRemove(input.item, input.amount);
            }

            // Start processing timer
            float speedMult = machineData.processingSpeed > 0f ? machineData.processingSpeed : 1f;
            _processingDuration = currentRecipe.craftingDuration / speedMult;
            _processingTimer = 0f;
            _isProcessing = true;
        }

        private void CompleteProcessing()
        {
            _isProcessing = false;
            _recheckRecipe = true; // immediately try the next cycle

            if (currentRecipe == null) return;

            // Produce outputs (always succeeds — factory precision)
            foreach (var output in currentRecipe.outputs)
            {
                if (output.item == null) continue;
                _outputInventory.TryAdd(output.item, output.amount);
            }

            GameDataEvents.RaiseMachineProcessingCompleted(this, currentRecipe);
        }
    }
}
