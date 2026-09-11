using CultivationGame.Data;
using CultivationGame.Player;
using CultivationGame.Systems;
using NUnit.Framework;
using UnityEngine;

namespace CultivationGame.Tests
{
    public class CraftingRequirementsTests
    {
        [Test]
        public void IncompleteRecipesAndInsufficientDuplicateIngredientsCannotCraft()
        {
            var go = new GameObject("Crafting requirements test");
            var ore = RecipeTestHelper.CreateItem("Ore");
            var output = RecipeTestHelper.CreateItem("Output");
            var recipe = RecipeTestHelper.CreateRecipe("Duplicate input",
                new[] { new RecipeIngredient { item = ore, amount = 2 }, new RecipeIngredient { item = ore, amount = 2 } },
                new[] { new RecipeIngredient { item = output, amount = 1 } });
            try
            {
                var inventory = go.AddComponent<PlayerInventory>();
                var crafting = go.AddComponent<CraftingSystem>();
                crafting.playerInventory = inventory;
                inventory.AddItem(ore, 2);
                Assert.That(crafting.CanCraft(recipe), Is.False);
                inventory.AddItem(ore, 2);
                Assert.That(crafting.CanCraft(recipe), Is.True);
                recipe.outputs.Clear();
                Assert.That(crafting.CanCraft(recipe), Is.False);
                Assert.That(inventory.HasItem(ore, 4), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(ore);
                Object.DestroyImmediate(output);
            }
        }
    }
}
