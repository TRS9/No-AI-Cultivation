using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;

namespace CultivationGame.UI
{
    public class CraftingUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject craftingPanel;
        public Transform recipeListContainer;
        public GameObject recipeSlotPrefab;

        [Header("Detail View")]
        public TextMeshProUGUI recipeNameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI inputsText;
        public TextMeshProUGUI outputsText;
        public Button craftButton;
        public Slider progressBar;

        [Header("References")]
        public RecipeDatabase recipeDatabase;
        public MachineType filterMachine = MachineType.None;

        private readonly List<RecipeSlotUI> _slots = new();
        private RecipeData _selectedRecipe;
        private bool _isCrafting;

        private void Awake()
        {
            GameEvents.OnInventoryChanged  += OnInventoryChanged;
            GameEvents.OnCraftingStarted   += OnCraftingStarted;
            GameEvents.OnCraftingCompleted += OnCraftingDone;
            GameEvents.OnCraftingFailed    += OnCraftingDone;
        }

        private void OnDestroy()
        {
            GameEvents.OnInventoryChanged  -= OnInventoryChanged;
            GameEvents.OnCraftingStarted   -= OnCraftingStarted;
            GameEvents.OnCraftingCompleted -= OnCraftingDone;
            GameEvents.OnCraftingFailed    -= OnCraftingDone;
        }

        public void Open()
        {
            craftingPanel.SetActive(true);
            BuildRecipeList();
        }

        public void Close() => craftingPanel.SetActive(false);

        private void BuildRecipeList()
        {
            foreach (var s in _slots) Destroy(s.gameObject);
            _slots.Clear();

            var recipes = recipeDatabase?.GetRecipesForMachine(filterMachine) ?? new List<RecipeData>();
            foreach (var recipe in recipes)
            {
                var go = Instantiate(recipeSlotPrefab, recipeListContainer);
                var slot = go.GetComponent<RecipeSlotUI>();
                bool canCraft = CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(recipe);
                slot.Setup(recipe, canCraft, SelectRecipe);
                _slots.Add(slot);
            }

            if (_selectedRecipe == null && recipes.Count > 0)
                SelectRecipe(recipes[0]);
        }

        private void SelectRecipe(RecipeData recipe)
        {
            _selectedRecipe = recipe;
            recipeNameText.text = recipe.recipeName;
            descriptionText.text = recipe.description;

            var sb = new StringBuilder();
            foreach (var i in recipe.inputs)
                sb.AppendLine($"  {i.item?.name ?? "?"} x{i.amount}");
            inputsText.text = sb.ToString();

            sb.Clear();
            foreach (var o in recipe.outputs)
                sb.AppendLine($"  {o.item?.name ?? "?"} x{o.amount}");
            outputsText.text = sb.ToString();

            bool canCraft = !_isCrafting && CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(recipe);
            craftButton.interactable = canCraft;
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(OnCraftButtonPressed);

            progressBar.gameObject.SetActive(false);
        }

        private void OnInventoryChanged()
        {
            if (craftingPanel == null || !craftingPanel.activeSelf) return;

            // Refresh craftable state on all slots
            var recipes = recipeDatabase?.GetRecipesForMachine(filterMachine) ?? new List<RecipeData>();
            for (int i = 0; i < _slots.Count && i < recipes.Count; i++)
            {
                bool canCraft = CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(recipes[i]);
                _slots[i].SetCraftable(canCraft);
            }

            if (_selectedRecipe != null) SelectRecipe(_selectedRecipe);
        }

        private void OnCraftButtonPressed()
        {
            if (_selectedRecipe == null || _isCrafting) return;
            CraftingSystem.Instance.TryCraft(_selectedRecipe, success =>
            {
                _isCrafting = false;
                progressBar.gameObject.SetActive(false);
                OnInventoryChanged();
            });
        }

        private void OnCraftingStarted(RecipeData recipe)
        {
            if (recipe != _selectedRecipe) return;
            _isCrafting = true;
            craftButton.interactable = false;
            if (recipe.craftingDuration > 0f)
            {
                progressBar.gameObject.SetActive(true);
                StartCoroutine(AnimateProgress(recipe.craftingDuration));
            }
        }

        private void OnCraftingDone(RecipeData recipe) { }

        private IEnumerator AnimateProgress(float duration)
        {
            float elapsed = 0f;
            progressBar.value = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                progressBar.value = elapsed / duration;
                yield return null;
            }
            progressBar.value = 1f;
        }
    }
}
