using System.Collections;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Core machine component. Handles input/output inventories, recipe selection,
    /// and timer-based processing. Attach to any placed machine prefab.
    /// </summary>
    public class BaseMachine : MonoBehaviour, IInteractable
    {
        [Header("Machine Configuration")]
        [SerializeField] private MachineData machineData;

        [Header("Recipe")]
        [SerializeField] private RecipeData currentRecipe;

        [Header("Inventory Capacities")]
        [SerializeField] private int inputCapacity = 50;
        [SerializeField] private int outputCapacity = 50;

        private MachineInventory _inputInventory;
        private MachineInventory _outputInventory;
        private float _processingTimer;
        private float _processingDuration;
        private bool _isProcessing;

        // --- Public API ---
        public MachineData MachineData => machineData;
        public RecipeData CurrentRecipe => currentRecipe;
        public MachineInventory InputInventory => _inputInventory;
        public MachineInventory OutputInventory => _outputInventory;
        public bool IsProcessing => _isProcessing;
        public float ProcessingProgress => _processingDuration > 0f
            ? Mathf.Clamp01(_processingTimer / _processingDuration) : 0f;

        private void Awake()
        {
            _inputInventory = new MachineInventory(inputCapacity);
            _outputInventory = new MachineInventory(outputCapacity);
        }

        private void Update()
        {
            if (_isProcessing)
            {
                _processingTimer += Time.deltaTime;
                if (_processingTimer >= _processingDuration)
                {
                    CompleteProcessing();
                }
            }
            else
            {
                TryStartProcessing();
            }
        }

        public void SetMachineData(MachineData data)
        {
            machineData = data;
        }

        public void SetRecipe(RecipeData recipe)
        {
            currentRecipe = recipe;
        }

        public void Interact(GameObject user)
        {
            GameDataEvents.RaiseMachineInteracted(this);
        }

        // --- Processing Logic ---

        private void TryStartProcessing()
        {
            if (currentRecipe == null || machineData == null) return;

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

            if (currentRecipe == null) return;

            // Produce outputs
            foreach (var output in currentRecipe.outputs)
            {
                if (output.item == null) continue;
                _outputInventory.TryAdd(output.item, output.amount);
            }

            GameDataEvents.RaiseMachineProcessingCompleted(this, currentRecipe);
        }
    }
}
