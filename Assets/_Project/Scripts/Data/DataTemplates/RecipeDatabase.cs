using System.Collections.Generic;
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
    }
}
