using System;
using System.Collections;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Player;

namespace CultivationGame.Systems
{
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        public PlayerInventory playerInventory;
        public PlayerStats playerStats;

        private bool _isCrafting;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public bool CanCraft(RecipeData recipe)
        {
            if (recipe == null || _isCrafting) return false;
            if (playerStats != null && playerStats.currentQi < recipe.qiCost) return false;
            if (recipe.requiredRealm != null && playerStats != null &&
                playerStats.currentRealm != null &&
                playerStats.currentRealm.realmIndex < recipe.requiredRealm.realmIndex) return false;

            var items = playerInventory.GetItems();
            foreach (var ingredient in recipe.inputs)
            {
                if (ingredient.item == null) continue;
                if (!items.TryGetValue(ingredient.item, out int count) || count < ingredient.amount)
                    return false;
            }
            return true;
        }

        public void TryCraft(RecipeData recipe, Action<bool> onComplete = null)
        {
            if (!CanCraft(recipe)) { onComplete?.Invoke(false); return; }
            StartCoroutine(CraftCoroutine(recipe, onComplete));
        }

        private IEnumerator CraftCoroutine(RecipeData recipe, Action<bool> onComplete)
        {
            _isCrafting = true;
            GameEvents.RaiseCraftingStarted(recipe);

            if (recipe.craftingDuration > 0f)
                yield return new WaitForSeconds(recipe.craftingDuration);

            if (recipe.qiCost > 0)
                GameEvents.RaiseAddQi(-recipe.qiCost);

            foreach (var ingredient in recipe.inputs)
                RemoveItems(ingredient.item, ingredient.amount);

            bool success = recipe.successRate >= 1f || UnityEngine.Random.value <= recipe.successRate;

            if (success)
            {
                foreach (var output in recipe.outputs)
                    for (int i = 0; i < output.amount; i++)
                        playerInventory.AddItem(output.item);

                GameEvents.RaiseCraftingCompleted(recipe);
            }
            else
            {
                GameEvents.RaiseCraftingFailed(recipe);
            }

            _isCrafting = false;
            onComplete?.Invoke(success);
        }

        private void RemoveItems(ItemData item, int amount)
        {
            if (item == null) return;
            var items = playerInventory.GetItems();
            if (!items.ContainsKey(item)) return;
            items[item] -= amount;
            if (items[item] <= 0) items.Remove(item);
            GameEvents.RaiseInventoryChanged();
        }
    }
}
