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
            if (recipe == null || _isCrafting || playerInventory == null) return false;
            return HasRequirements(recipe);
        }

        /// <summary>
        /// Checks qi, realm, and ingredients. Used both before starting a craft and
        /// again when it finishes — the player may have spent ingredients or qi
        /// during the crafting duration.
        /// </summary>
        private bool HasRequirements(RecipeData recipe)
        {
            if (playerStats != null && playerStats.currentQi < recipe.qiCost) return false;
            if (recipe.requiredRealm != null && playerStats != null &&
                playerStats.currentRealm != null &&
                playerStats.currentRealm.realmIndex < recipe.requiredRealm.realmIndex) return false;

            foreach (var ingredient in recipe.inputs)
            {
                if (ingredient.item == null) continue;
                if (!playerInventory.HasItem(ingredient.item, ingredient.amount))
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
            GameDataEvents.RaiseCraftingStarted(recipe);

            if (recipe.craftingDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < recipe.craftingDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / recipe.craftingDuration);
                    GameDataEvents.RaiseCraftingProgressChanged(recipe, progress);
                    yield return null;
                }
            }

            // Re-validate — ingredients/qi may have been consumed elsewhere while
            // the craft was running. Never deduct what the player no longer has.
            if (!HasRequirements(recipe))
            {
                _isCrafting = false;
                GameDataEvents.RaiseCraftingFailed(recipe);
                onComplete?.Invoke(false);
                yield break;
            }

            if (recipe.qiCost > 0)
                GameEvents.RaiseAddQi(-recipe.qiCost);

            foreach (var ingredient in recipe.inputs)
            {
                if (ingredient.item == null) continue;
                playerInventory.RemoveItem(ingredient.item, ingredient.amount);
            }

            bool success = recipe.successRate >= 1f || UnityEngine.Random.value <= recipe.successRate;

            if (success)
            {
                foreach (var output in recipe.outputs)
                    playerInventory.AddItem(output.item, output.amount);

                GameDataEvents.RaiseCraftingCompleted(recipe);
            }
            else
            {
                GameDataEvents.RaiseCraftingFailed(recipe);
            }

            _isCrafting = false;
            onComplete?.Invoke(success);
        }
    }
}
