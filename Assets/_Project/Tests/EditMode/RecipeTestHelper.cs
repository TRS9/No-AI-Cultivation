using UnityEngine;
using CultivationGame.Data;

namespace CultivationGame.Tests
{
    /// <summary>
    /// Concrete ItemData subclass used exclusively in tests.
    /// </summary>
    public class TestItemData : ItemData { }

    /// <summary>
    /// Helpers for creating mock ScriptableObject instances in EditMode tests.
    /// </summary>
    public static class RecipeTestHelper
    {
        public static TestItemData CreateItem(string itemName)
        {
            var item = ScriptableObject.CreateInstance<TestItemData>();
            item.name = itemName;
            return item;
        }

        public static RecipeData CreateRecipe(
            string recipeName,
            RecipeIngredient[] inputs,
            RecipeIngredient[] outputs,
            CultivationGame.Core.MachineType machine = CultivationGame.Core.MachineType.Furnace,
            float duration = 5f)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.recipeName = recipeName;
            recipe.inputs = inputs != null ? new System.Collections.Generic.List<RecipeIngredient>(inputs) : null;
            recipe.outputs = outputs != null ? new System.Collections.Generic.List<RecipeIngredient>(outputs) : null;
            recipe.requiredMachine = machine;
            recipe.craftingDuration = duration;
            recipe.successRate = 1f;
            return recipe;
        }

        public static RecipeDatabase CreateDatabase(params RecipeData[] recipes)
        {
            var db = ScriptableObject.CreateInstance<RecipeDatabase>();
            db.allRecipes = new System.Collections.Generic.List<RecipeData>(recipes);
            return db;
        }
    }
}
