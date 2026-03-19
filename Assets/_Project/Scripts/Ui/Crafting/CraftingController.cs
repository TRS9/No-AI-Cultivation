using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.UI
{
    public class CraftingController : MonoBehaviour
    {
        [SerializeField] private CraftingDataSource craftingData;
        [SerializeField] private RecipeDatabase recipeDatabase;

        private VisualElement _panel;
        private ListView _recipeList;
        private Button _craftBtn;
        private ProgressBar _craftProgress;

        public void InitializeUI(VisualElement root)
        {
            _panel = root.Q<VisualElement>("CraftingPanel");
            _recipeList = root.Q<ListView>("RecipeList");
            _craftBtn = root.Q<Button>("CraftBtn");
            _craftProgress = root.Q<ProgressBar>("CraftProgress");

            _craftBtn?.RegisterCallback<ClickEvent>(e => craftingData?.TryCraftSelected());
            _panel?.Q<Button>("CloseCraftingBtn")
                ?.RegisterCallback<ClickEvent>(e => GameStateManager.Instance?.ClosePanel("Crafting"));

            SetupRecipeList();

            if (craftingData != null)
            {
                craftingData.RecipeDatabase = recipeDatabase;
                craftingData.ResetState();
                craftingData.Subscribe();

                var craftingRight = root.Q<VisualElement>("CraftingRight");
                craftingRight.dataSource = craftingData;

                root.Q<Label>("RecipeName")?.SetBinding("text", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(CraftingDataSource.SelectedRecipeName)),
                    bindingMode = BindingMode.ToTarget
                });

                root.Q<Label>("RecipeDesc")?.SetBinding("text", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(CraftingDataSource.SelectedRecipeDescription)),
                    bindingMode = BindingMode.ToTarget
                });

                root.Q<Label>("RecipeInputs")?.SetBinding("text", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(CraftingDataSource.InputsText)),
                    bindingMode = BindingMode.ToTarget
                });

                root.Q<Label>("RecipeOutputs")?.SetBinding("text", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(CraftingDataSource.OutputsText)),
                    bindingMode = BindingMode.ToTarget
                });

                _craftProgress?.SetBinding("value", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(CraftingDataSource.CraftProgress)),
                    bindingMode = BindingMode.ToTarget
                });

                craftingData.PropertyChanged += OnPropertyChanged;
            }

            GameEvents.OnCraftingStationInteracted += OnStationInteracted;
            GameEvents.OnPanelStateChanged += OnPanelStateChanged;
        }

        private void OnDisable()
        {
            if (craftingData != null)
            {
                craftingData.Unsubscribe();
                craftingData.PropertyChanged -= OnPropertyChanged;
            }
            GameEvents.OnCraftingStationInteracted -= OnStationInteracted;
            GameEvents.OnPanelStateChanged -= OnPanelStateChanged;
        }

        private void OnStationInteracted() => GameStateManager.Instance?.OpenPanel("Crafting");

        private void OnPanelStateChanged(string panelId, bool isOpen)
        {
            if (panelId != "Crafting") return;

            if (isOpen)
            {
                craftingData?.BuildRecipeList();
                if (_panel != null) _panel.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (_panel != null) _panel.style.display = DisplayStyle.None;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CraftingDataSource.CanCraft):
                    if (_craftBtn != null) _craftBtn.SetEnabled(craftingData.CanCraft);
                    break;

                case nameof(CraftingDataSource.IsCrafting):
                    if (_craftProgress != null)
                        _craftProgress.style.display = craftingData.IsCrafting
                            ? DisplayStyle.Flex : DisplayStyle.None;
                    break;

                case nameof(CraftingDataSource.Recipes):
                    if (_recipeList != null)
                    {
                        _recipeList.itemsSource = craftingData.Recipes;
                        _recipeList.Rebuild();
                    }
                    break;
            }
        }

        private void SetupRecipeList()
        {
            if (_recipeList == null) return;

            _recipeList.makeItem = () =>
            {
                var slot = new Label();
                slot.AddToClassList("recipe-slot");
                return slot;
            };

            _recipeList.bindItem = (element, index) =>
            {
                if (index >= craftingData.Recipes.Count) return;
                var data = craftingData.Recipes[index];
                if (element is Label label) label.text = data.Name;
                element.EnableInClassList("recipe-slot--disabled", !data.CanCraft);
            };

            _recipeList.selectionChanged += items =>
            {
                foreach (var item in items)
                {
                    if (item is RecipeSlotData slot)
                        craftingData.SelectRecipe(slot.Recipe);
                }
            };

            _recipeList.itemsSource = craftingData?.Recipes;
        }
    }
}
