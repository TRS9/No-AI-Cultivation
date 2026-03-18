using System;
using System.Collections.Generic;
using System.ComponentModel; // Hinzugefügt für INotifyPropertyChanged
using System.Text;
using UnityEngine;
using Unity.Properties;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;

namespace CultivationGame.UI
{
    [Serializable]
    public struct RecipeSlotData
    {
        public RecipeData Recipe;
        public string Name;
        public bool CanCraft;
    }

    [CreateAssetMenu(menuName = "Cultivation/UI/Crafting Data Source")]
    // Geändert: INotifyPropertyChanged
    public class CraftingDataSource : ScriptableObject, INotifyPropertyChanged
    {
        // Geändert: Standard PropertyChangedEventHandler
        public event PropertyChangedEventHandler PropertyChanged;

        private string _selectedRecipeName = "";
        private string _selectedRecipeDescription = "";
        private string _inputsText = "";
        private string _outputsText = "";
        private bool _canCraft;
        private bool _isCrafting;
        private float _craftProgress;

        [CreateProperty]
        public string SelectedRecipeName
        {
            get => _selectedRecipeName;
            private set
            {
                if (_selectedRecipeName == value) return;
                _selectedRecipeName = value;
                Notify(nameof(SelectedRecipeName));
            }
        }

        [CreateProperty]
        public string SelectedRecipeDescription
        {
            get => _selectedRecipeDescription;
            private set
            {
                if (_selectedRecipeDescription == value) return;
                _selectedRecipeDescription = value;
                Notify(nameof(SelectedRecipeDescription));
            }
        }

        [CreateProperty]
        public string InputsText
        {
            get => _inputsText;
            private set
            {
                if (_inputsText == value) return;
                _inputsText = value;
                Notify(nameof(InputsText));
            }
        }

        [CreateProperty]
        public string OutputsText
        {
            get => _outputsText;
            private set
            {
                if (_outputsText == value) return;
                _outputsText = value;
                Notify(nameof(OutputsText));
            }
        }

        [CreateProperty] // Hinzugefügt für Binding im UI Builder (z.B. Button Enable/Disable)
        public bool CanCraft
        {
            get => _canCraft;
            private set
            {
                if (_canCraft == value) return;
                _canCraft = value;
                Notify(nameof(CanCraft));
            }
        }

        [CreateProperty] // Hinzugefügt für Binding
        public bool IsCrafting
        {
            get => _isCrafting;
            private set
            {
                if (_isCrafting == value) return;
                _isCrafting = value;
                Notify(nameof(IsCrafting));
            }
        }

        [CreateProperty]
        public float CraftProgress
        {
            get => _craftProgress;
            private set
            {
                if (Mathf.Approximately(_craftProgress, value)) return;
                _craftProgress = value;
                Notify(nameof(CraftProgress));
            }
        }

        [CreateProperty] // Hinzugefügt für Binding der ListView
        public List<RecipeSlotData> Recipes { get; private set; } = new();

        public RecipeData SelectedRecipe { get; private set; }

        [NonSerialized] public RecipeDatabase RecipeDatabase;
        public MachineType FilterMachine = MachineType.None;

        // Geändert: Nutzt jetzt PropertyChangedEventArgs
        private void Notify(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Subscribe()
        {
            GameEvents.OnInventoryChanged += RefreshCraftableState;
            GameDataEvents.OnCraftingStarted += HandleCraftingStarted;
            GameDataEvents.OnCraftingCompleted += HandleCraftingDone;
            GameDataEvents.OnCraftingFailed += HandleCraftingDone;
            GameDataEvents.OnCraftingProgressChanged += HandleCraftingProgress;
        }

        public void Unsubscribe()
        {
            GameEvents.OnInventoryChanged -= RefreshCraftableState;
            GameDataEvents.OnCraftingStarted -= HandleCraftingStarted;
            GameDataEvents.OnCraftingCompleted -= HandleCraftingDone;
            GameDataEvents.OnCraftingFailed -= HandleCraftingDone;
            GameDataEvents.OnCraftingProgressChanged -= HandleCraftingProgress;
        }

        public void ResetState()
        {
            SelectedRecipe = null;
            SelectedRecipeName = "";
            SelectedRecipeDescription = "";
            InputsText = "";
            OutputsText = "";
            CanCraft = false;
            IsCrafting = false;
            CraftProgress = 0f;
            Recipes.Clear();
            Notify(nameof(Recipes));
        }

        public void BuildRecipeList()
        {
            Recipes.Clear();
            var recipes = RecipeDatabase != null
                ? RecipeDatabase.GetRecipesForMachine(FilterMachine)
                : new List<RecipeData>();

            foreach (var recipe in recipes)
            {
                bool canCraft = CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(recipe);
                Recipes.Add(new RecipeSlotData
                {
                    Recipe = recipe,
                    Name = recipe.recipeName,
                    CanCraft = canCraft
                });
            }

            Notify(nameof(Recipes));

            if (SelectedRecipe == null && Recipes.Count > 0)
                SelectRecipe(Recipes[0].Recipe);
        }

        public void SelectRecipe(RecipeData recipe)
        {
            SelectedRecipe = recipe;
            SelectedRecipeName = recipe.recipeName;
            SelectedRecipeDescription = recipe.description;

            var sb = new StringBuilder();
            foreach (var i in recipe.inputs)
                sb.AppendLine($"  {i.item?.name ?? "?"} x{i.amount}");
            InputsText = sb.ToString();

            sb.Clear();
            foreach (var o in recipe.outputs)
                sb.AppendLine($"  {o.item?.name ?? "?"} x{o.amount}");
            OutputsText = sb.ToString();

            CanCraft = !IsCrafting && CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(recipe);
        }

        private void RefreshCraftableState()
        {
            for (int i = 0; i < Recipes.Count; i++)
            {
                var slot = Recipes[i];
                slot.CanCraft = CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(slot.Recipe);
                Recipes[i] = slot;
            }
            Notify(nameof(Recipes));

            if (SelectedRecipe != null)
                SelectRecipe(SelectedRecipe);
        }

        private void HandleCraftingStarted(RecipeData recipe)
        {
            IsCrafting = true;
            CanCraft = false;
            CraftProgress = 0f;
        }

        private void HandleCraftingDone(RecipeData recipe)
        {
            IsCrafting = false;
            CraftProgress = 0f;
            RefreshCraftableState();
        }

        private void HandleCraftingProgress(RecipeData recipe, float normalizedProgress)
        {
            CraftProgress = normalizedProgress * 100f;
        }

        public void TryCraftSelected()
        {
            if (SelectedRecipe == null || IsCrafting) return;
            CraftingSystem.Instance?.TryCraft(SelectedRecipe);
        }
    }
}