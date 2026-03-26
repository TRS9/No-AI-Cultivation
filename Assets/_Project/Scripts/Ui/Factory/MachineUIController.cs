using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;
using CultivationGame.Player;

namespace CultivationGame.UI
{
    /// <summary>
    /// Controls the Machine Interaction panel that opens when a player interacts
    /// with a placed machine. Shows input/output slots, recipe selection, and
    /// processing progress.
    /// </summary>
    public class MachineUIController : MonoBehaviour
    {
        [SerializeField] private RecipeDatabase recipeDatabase;
        [SerializeField] private PlayerInventory playerInventory;

        private VisualElement _panel;
        private Label _machineNameLabel;
        private VisualElement _machineIcon;
        private VisualElement _inputSlotsContainer;
        private VisualElement _outputSlotsContainer;
        private DropdownField _recipeDropdown;
        private ProgressBar _progressBar;
        private Button _closeButton;

        private BaseMachine _currentMachine;
        private List<RecipeData> _availableRecipes = new();

        // ------------------------------------------------------------------ //
        //  Lifecycle
        // ------------------------------------------------------------------ //

        public void InitializeUI(VisualElement root)
        {
            _panel = root.Q<VisualElement>("MachineUIPanel");
            if (_panel == null) return;

            _machineNameLabel = _panel.Q<Label>("MachineName");
            _machineIcon = _panel.Q<VisualElement>("MachineIcon");
            _inputSlotsContainer = _panel.Q<VisualElement>("InputSlots");
            _outputSlotsContainer = _panel.Q<VisualElement>("OutputSlots");
            _recipeDropdown = _panel.Q<DropdownField>("RecipeDropdown");
            _progressBar = _panel.Q<ProgressBar>("ProcessingProgress");
            _closeButton = _panel.Q<Button>("CloseButton");

            _panel.style.display = DisplayStyle.None;

            if (_closeButton != null)
                _closeButton.clicked += Close;

            if (_recipeDropdown != null)
                _recipeDropdown.RegisterValueChangedCallback(OnRecipeSelected);

            GameDataEvents.OnMachineInteracted += OnMachineInteracted;
        }

        private void OnDisable()
        {
            GameDataEvents.OnMachineInteracted -= OnMachineInteracted;
        }

        private void Update()
        {
            if (_currentMachine == null || _panel == null) return;
            if (_panel.style.display == DisplayStyle.None) return;

            UpdateProgress();
            UpdateSlots();
        }

        // ------------------------------------------------------------------ //
        //  Event handlers
        // ------------------------------------------------------------------ //

        private void OnMachineInteracted(MonoBehaviour machine)
        {
            if (machine is BaseMachine baseMachine)
                Open(baseMachine);
        }

        private void OnRecipeSelected(ChangeEvent<string> evt)
        {
            if (_currentMachine == null || _availableRecipes == null) return;

            int index = _recipeDropdown.index;
            if (index >= 0 && index < _availableRecipes.Count)
                _currentMachine.SetRecipe(_availableRecipes[index]);
        }

        // ------------------------------------------------------------------ //
        //  Open / Close
        // ------------------------------------------------------------------ //

        public void Open(BaseMachine machine)
        {
            if (_panel == null || machine == null) return;

            _currentMachine = machine;
            _panel.style.display = DisplayStyle.Flex;

            // Machine header
            if (_machineNameLabel != null)
            {
                string name = machine.MachineData != null ? machine.MachineData.machineName : "Machine";
                _machineNameLabel.text = name;
            }

            if (_machineIcon != null && machine.MachineData != null && machine.MachineData.icon != null)
                _machineIcon.style.backgroundImage = new StyleBackground(machine.MachineData.icon);

            // Populate recipe dropdown
            PopulateRecipes();

            // Initial slot display
            UpdateSlots();
            UpdateProgress();
        }

        public void Close()
        {
            if (_panel != null)
                _panel.style.display = DisplayStyle.None;

            _currentMachine = null;
        }

        // ------------------------------------------------------------------ //
        //  Recipe dropdown
        // ------------------------------------------------------------------ //

        private void PopulateRecipes()
        {
            if (_recipeDropdown == null || _currentMachine == null) return;

            _availableRecipes.Clear();

            if (recipeDatabase != null && _currentMachine.MachineData != null)
                _availableRecipes = recipeDatabase.GetRecipesForMachine(_currentMachine.MachineData.machineType);

            var choices = new List<string>();
            int selectedIndex = -1;

            for (int i = 0; i < _availableRecipes.Count; i++)
            {
                choices.Add(_availableRecipes[i].recipeName);
                if (_currentMachine.CurrentRecipe == _availableRecipes[i])
                    selectedIndex = i;
            }

            _recipeDropdown.choices = choices;
            _recipeDropdown.index = selectedIndex >= 0 ? selectedIndex : (choices.Count > 0 ? 0 : -1);

            // Auto-select first recipe if none is set
            if (_currentMachine.CurrentRecipe == null && _availableRecipes.Count > 0)
                _currentMachine.SetRecipe(_availableRecipes[0]);
        }

        // ------------------------------------------------------------------ //
        //  Slot display & transfer
        // ------------------------------------------------------------------ //

        private void UpdateSlots()
        {
            if (_currentMachine == null) return;

            RebuildSlotContainer(_inputSlotsContainer, _currentMachine.InputInventory, true);
            RebuildSlotContainer(_outputSlotsContainer, _currentMachine.OutputInventory, false);
        }

        private void RebuildSlotContainer(VisualElement container, MachineInventory inventory, bool isInput)
        {
            if (container == null || inventory == null) return;
            container.Clear();

            var snapshot = inventory.GetSnapshot();

            foreach (var kv in snapshot)
            {
                var slot = new Button();
                slot.AddToClassList("machine-slot");

                if (kv.Key.icon != null)
                {
                    var icon = new VisualElement();
                    icon.AddToClassList("machine-slot__icon");
                    icon.style.backgroundImage = new StyleBackground(kv.Key.icon);
                    slot.Add(icon);
                }

                var label = new Label($"{kv.Key.name} x{kv.Value}");
                label.AddToClassList("machine-slot__label");
                slot.Add(label);

                // Click to transfer
                var item = kv.Key;
                if (isInput)
                {
                    // Click input slot → return item to player
                    slot.clicked += () => TransferToPlayer(item);
                }
                else
                {
                    // Click output slot → take item to player
                    slot.clicked += () => TransferToPlayer(item);
                }

                container.Add(slot);
            }

            // For input container, add an "Add Item" button
            if (isInput)
            {
                var addButton = new Button();
                addButton.text = "+";
                addButton.AddToClassList("machine-slot--add");
                addButton.clicked += () => TransferFromPlayer();
                container.Add(addButton);
            }
        }

        /// <summary>
        /// Transfer one item from machine output/input to player inventory.
        /// </summary>
        private void TransferToPlayer(ItemData item)
        {
            if (_currentMachine == null || playerInventory == null || item == null) return;

            // Try output first, then input
            int removed = _currentMachine.OutputInventory.TryRemove(item, 1);
            if (removed == 0)
                removed = _currentMachine.InputInventory.TryRemove(item, 1);

            if (removed > 0)
            {
                playerInventory.AddItem(item);
                GameEvents.RaiseInventoryChanged();
                UpdateSlots();
            }
        }

        /// <summary>
        /// Transfer the first matching item from player inventory to machine input.
        /// Uses the current recipe's inputs to determine what's needed.
        /// </summary>
        private void TransferFromPlayer()
        {
            if (_currentMachine == null || playerInventory == null) return;

            var recipe = _currentMachine.CurrentRecipe;
            if (recipe == null) return;

            var playerItems = playerInventory.GetItems();

            foreach (var input in recipe.inputs)
            {
                if (input.item == null) continue;
                if (playerItems.TryGetValue(input.item, out int count) && count > 0)
                {
                    if (_currentMachine.InputInventory.HasSpace())
                    {
                        playerItems[input.item]--;
                        if (playerItems[input.item] <= 0) playerItems.Remove(input.item);
                        _currentMachine.InputInventory.TryAdd(input.item, 1);
                        GameEvents.RaiseInventoryChanged();
                        UpdateSlots();
                        return;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ //
        //  Progress bar
        // ------------------------------------------------------------------ //

        private void UpdateProgress()
        {
            if (_progressBar == null || _currentMachine == null) return;

            float progress = _currentMachine.ProcessingProgress;
            _progressBar.value = progress * 100f;
            _progressBar.title = _currentMachine.IsProcessing
                ? $"{Mathf.RoundToInt(progress * 100)}%"
                : "Idle";
        }
    }
}
