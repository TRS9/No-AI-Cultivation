using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data
{
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Cultivation/Recipe Database")]
    public class RecipeDatabase : ScriptableObject
    {
        public List<RecipeData> allRecipes;

        public List<RecipeData> GetRecipesForMachine(MachineType machine)
        {
            return allRecipes?.FindAll(r => r.requiredMachine == machine) ?? new List<RecipeData>();
        }

        public List<RecipeData> GetUnlockedRecipes(RealmDefinition currentRealm)
        {
            return allRecipes?.FindAll(r =>
                r.requiredRealm == null ||
                currentRealm == null ||
                r.requiredRealm.realmIndex <= currentRealm.realmIndex)
                ?? new List<RecipeData>();
        }

        /// <summary>
        /// Returns all recipes in the database.
        /// </summary>
        public List<RecipeData> GetAllRecipes()
        {
            return allRecipes != null ? new List<RecipeData>(allRecipes) : new List<RecipeData>();
        }

        /// <summary>
        /// Finds the first recipe that exactly matches the given inputs and machine type.
        /// Returns null if no match is found.
        /// </summary>
        public RecipeData FindRecipe(List<RecipeIngredient> inputs, MachineType machineType)
        {
            if (allRecipes == null || inputs == null)
                return null;

            return allRecipes.FirstOrDefault(r =>
                r.requiredMachine == machineType &&
                r.inputs != null &&
                r.inputs.Count == inputs.Count &&
                r.inputs.All(ri => inputs.Any(i =>
                    i.item == ri.item && i.amount == ri.amount)) &&
                inputs.All(i => r.inputs.Any(ri =>
                    ri.item == i.item && ri.amount == i.amount)));
        }
    }
}
