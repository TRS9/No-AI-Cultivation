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
    /// Inspect panel that opens when a player presses E on a placed machine.
    /// Shows real-time status (powered/unpowered), processing progress with time,
    /// input/output inventories, recipe switching, and a "Leeren" (empty) button.
    /// Managed by GameStateManager with panel ID "MachineInspect".
    /// </summary>
    public class MachineInspectUI : MonoBehaviour
    {
        private const string PanelId = "MachineInspect";

        [SerializeField] private RecipeDatabase recipeDatabase;
        [SerializeField] private PlayerInventory playerInventory;

        // --- UI Elements ---
        private VisualElement _panel;
        private Label _machineNameLabel;
        private Label _statusLabel;
        private ProgressBar _progressBar;
        private Label _recipeLabel;
        private Label _timeLabel;
        private VisualElement _inputContainer;
        private VisualElement _outputContainer;
        private DropdownField _recipeDropdown;
        private Button _emptyButton;
        private Button _closeButton;

        // --- State ---
        private BaseMachine _currentMachine;
        private List<RecipeData> _availableRecipes = new();
        private bool _slotsDirty;

        // ------------------------------------------------------------------ //
        //  Lifecycle
        // ------------------------------------------------------------------ //

        public void InitializeUI(VisualElement root)
        {
            _panel = root.Q<VisualElement>("MachineInspectPanel");
            if (_panel == null) return;

            _machineNameLabel = _panel.Q<Label>("InspectMachineName");
            _statusLabel = _panel.Q<Label>("InspectStatus");
            _progressBar = _panel.Q<ProgressBar>("InspectProgress");
            _recipeLabel = _panel.Q<Label>("InspectRecipe");
            _timeLabel = _panel.Q<Label>("InspectTime");
            _inputContainer = _panel.Q<VisualElement>("InspectInputSlots");
            _outputContainer = _panel.Q<VisualElement>("InspectOutputSlots");
            _recipeDropdown = _panel.Q<DropdownField>("InspectRecipeDropdown");
            _emptyButton = _panel.Q<Button>("InspectEmptyButton");
            _closeButton = _panel.Q<Button>("InspectCloseButton");

            _panel.style.display = DisplayStyle.None;

            if (_closeButton != null)
                _closeButton.clicked += RequestClose;

            if (_emptyButton != null)
                _emptyButton.clicked += EmptyAllToPlayer;

            if (_recipeDropdown != null)
                _recipeDropdown.RegisterValueChangedCallback(OnRecipeSelected);

            GameDataEvents.OnMachineInteracted += OnMachineInteracted;
            GameEvents.OnPanelStateChanged += OnPanelStateChanged;
        }

        private void OnDisable()
        {
            GameDataEvents.OnMachineInteracted -= OnMachineInteracted;
            GameEvents.OnPanelStateChanged -= OnPanelStateChanged;
            UnsubscribeFromMachine();
        }

        private void Update()
        {
            if (_currentMachine == null || _panel == null) return;
            if (_panel.style.display == DisplayStyle.None) return;

            UpdateStatus();
            UpdateProgress();
            UpdateTime();

            if (_slotsDirty)
            {
                UpdateSlots();
                _slotsDirty = false;
            }
        }

        // ------------------------------------------------------------------ //
        //  Event handlers
        // ------------------------------------------------------------------ //

        private void OnMachineInteracted(MonoBehaviour machine)
        {
            if (machine is BaseMachine baseMachine)
            {
                _currentMachine = baseMachine;
                GameStateManager.Instance?.OpenPanel(PanelId);
            }
        }

        private void OnPanelStateChanged(string panelId, bool isOpen)
        {
            if (panelId != PanelId) return;

            if (isOpen)
                Open();
            else
                Close();
        }

        private void OnRecipeSelected(ChangeEvent<string> evt)
        {
            if (_currentMachine == null || _availableRecipes == null) return;

            int index = _recipeDropdown.index;
            if (index >= 0 && index < _availableRecipes.Count)
            {
                _currentMachine.SetRecipe(_availableRecipes[index]);
                UpdateRecipeLabel();
            }
        }

        private void MarkSlotsDirty() => _slotsDirty = true;

        // ------------------------------------------------------------------ //
        //  Open / Close
        // ------------------------------------------------------------------ //

        private void Open()
        {
            if (_panel == null || _currentMachine == null) return;

            _panel.style.display = DisplayStyle.Flex;

            // Machine header
            if (_machineNameLabel != null)
            {
                string name = _currentMachine.MachineData != null
                    ? _currentMachine.MachineData.machineName
                    : "Machine";
                _machineNameLabel.text = name;
            }

            SubscribeToMachine();
            PopulateRecipes();
            UpdateRecipeLabel();
            UpdateStatus();
            UpdateProgress();
            UpdateTime();

            _slotsDirty = true;
            UpdateSlots();
            _slotsDirty = false;
        }

        private void Close()
        {
            UnsubscribeFromMachine();

            if (_panel != null)
                _panel.style.display = DisplayStyle.None;

            _currentMachine = null;
        }

        private void RequestClose()
        {
            GameStateManager.Instance?.ClosePanel(PanelId);
        }

        private void SubscribeToMachine()
        {
            if (_currentMachine == null) return;
            if (_currentMachine.InputInventory != null)
                _currentMachine.InputInventory.OnChanged += MarkSlotsDirty;
            if (_currentMachine.OutputInventory != null)
                _currentMachine.OutputInventory.OnChanged += MarkSlotsDirty;
        }

        private void UnsubscribeFromMachine()
        {
            if (_currentMachine == null) return;
            if (_currentMachine.InputInventory != null)
                _currentMachine.InputInventory.OnChanged -= MarkSlotsDirty;
            if (_currentMachine.OutputInventory != null)
                _currentMachine.OutputInventory.OnChanged -= MarkSlotsDirty;
        }

        // ------------------------------------------------------------------ //
        //  Status display
        // ------------------------------------------------------------------ //

        private void UpdateStatus()
        {
            if (_statusLabel == null || _currentMachine == null) return;

            string powerIcon = _currentMachine.IsPowered ? "\u26A1" : "\u2B1C";

            if (_currentMachine.IsProcessing)
                _statusLabel.text = $"Status: {powerIcon} Verarbeitet...";
            else if (!_currentMachine.IsPowered)
                _statusLabel.text = $"Status: {powerIcon} Kein Strom";
            else
                _statusLabel.text = $"Status: {powerIcon} Bereit";
        }

        private void UpdateProgress()
        {
            if (_progressBar == null || _currentMachine == null) return;

            float progress = _currentMachine.ProcessingProgress;
            _progressBar.value = progress * 100f;
            _progressBar.title = _currentMachine.IsProcessing
                ? $"{Mathf.RoundToInt(progress * 100)}%"
                : "Idle";
        }

        private void UpdateTime()
        {
            if (_timeLabel == null || _currentMachine == null) return;

            if (_currentMachine.IsProcessing)
            {
                float current = _currentMachine.ProcessingTimer;
                float total = _currentMachine.ProcessingDuration;
                _timeLabel.text = $"Zeit: {current:F1}s / {total:F1}s";
            }
            else
            {
                _timeLabel.text = "Zeit: --";
            }
        }

        private void UpdateRecipeLabel()
        {
            if (_recipeLabel == null || _currentMachine == null) return;

            var recipe = _currentMachine.CurrentRecipe;
            if (recipe == null)
            {
                _recipeLabel.text = "Rezept: ---";
                return;
            }

            string inputNames = BuildIngredientNames(recipe.inputs);
            string outputNames = BuildIngredientNames(recipe.outputs);
            _recipeLabel.text = $"Rezept: {inputNames} \u2192 {outputNames}";
        }

        private static string BuildIngredientNames(List<RecipeIngredient> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0) return "---";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var ing = ingredients[i];
                string name = ing.item != null ? ing.item.name : "?";
                sb.Append(name);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ //
        //  Recipe dropdown
        // ------------------------------------------------------------------ //

        private void PopulateRecipes()
        {
            if (_recipeDropdown == null || _currentMachine == null) return;

            _availableRecipes.Clear();

            if (recipeDatabase != null && _currentMachine.MachineData != null)
                _availableRecipes = recipeDatabase.GetRecipesForMachine(
                    _currentMachine.MachineData.machineType);

            var choices = new List<string>();
            int selectedIndex = -1;

            for (int i = 0; i < _availableRecipes.Count; i++)
            {
                choices.Add(_availableRecipes[i].recipeName);
                if (_currentMachine.CurrentRecipe == _availableRecipes[i])
                    selectedIndex = i;
            }

            _recipeDropdown.choices = choices;
            _recipeDropdown.index = selectedIndex >= 0
                ? selectedIndex
                : (choices.Count > 0 ? 0 : -1);

            // Auto-select first recipe if none is set
            if (_currentMachine.CurrentRecipe == null && _availableRecipes.Count > 0)
                _currentMachine.SetRecipe(_availableRecipes[0]);
        }

        // ------------------------------------------------------------------ //
        //  Slot display
        // ------------------------------------------------------------------ //

        private void UpdateSlots()
        {
            if (_currentMachine == null) return;
            RebuildSlotContainer(_inputContainer, _currentMachine.InputInventory);
            RebuildSlotContainer(_outputContainer, _currentMachine.OutputInventory);
        }

        private void RebuildSlotContainer(VisualElement container, MachineInventory inventory)
        {
            if (container == null || inventory == null) return;
            container.Clear();

            var snapshot = inventory.GetSnapshot();
            int capacity = inventory.MaxCapacity;

            foreach (var kv in snapshot)
            {
                var slot = new VisualElement();
                slot.AddToClassList("inspect-slot");

                if (kv.Key.icon != null)
                {
                    var icon = new VisualElement();
                    icon.AddToClassList("inspect-slot__icon");
                    icon.style.backgroundImage = new StyleBackground(kv.Key.icon);
                    slot.Add(icon);
                }

                string text = capacity > 0
                    ? $"{kv.Key.name}  x{kv.Value}/{capacity}"
                    : $"{kv.Key.name}  x{kv.Value}";

                var label = new Label(text);
                label.AddToClassList("inspect-slot__label");
                slot.Add(label);

                container.Add(slot);
            }

            if (snapshot.Count == 0)
            {
                var empty = new Label("(leer)");
                empty.AddToClassList("inspect-slot__empty");
                container.Add(empty);
            }
        }

        // ------------------------------------------------------------------ //
        //  "Leeren" – empty all items to player
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Transfers all items from both input and output inventories to the player.
        /// </summary>
        private void EmptyAllToPlayer()
        {
            if (_currentMachine == null || playerInventory == null) return;

            TransferAllFromInventory(_currentMachine.InputInventory);
            TransferAllFromInventory(_currentMachine.OutputInventory);

            GameEvents.RaiseInventoryChanged();
            GameDataEvents.RaiseMachineInventoryChanged(_currentMachine);
        }

        private void TransferAllFromInventory(MachineInventory inventory)
        {
            if (inventory == null) return;

            var snapshot = inventory.GetSnapshot();
            foreach (var kv in snapshot)
            {
                int remaining = kv.Value;
                while (remaining > 0)
                {
                    int removed = inventory.TryRemove(kv.Key, remaining);
                    if (removed <= 0) break;

                    for (int i = 0; i < removed; i++)
                        playerInventory.AddItem(kv.Key);

                    remaining -= removed;
                }
            }
        }
    }
}
