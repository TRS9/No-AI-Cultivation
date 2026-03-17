using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Data;

namespace CultivationGame.UI
{
    public class RecipeSlotUI : MonoBehaviour
    {
        public TextMeshProUGUI recipeNameText;
        public Button selectButton;

        private RecipeData _recipe;
        private Action<RecipeData> _onSelect;

        public void Setup(RecipeData recipe, bool canCraft, Action<RecipeData> onSelect)
        {
            _recipe = recipe;
            _onSelect = onSelect;
            recipeNameText.text = recipe.recipeName;
            selectButton.interactable = canCraft;
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onSelect?.Invoke(_recipe));
        }

        public void SetCraftable(bool canCraft)
        {
            selectButton.interactable = canCraft;
        }
    }
}
