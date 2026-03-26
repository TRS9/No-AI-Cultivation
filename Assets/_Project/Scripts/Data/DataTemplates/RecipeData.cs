using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data
{
    [Serializable]
    public struct RecipeIngredient
    {
        public ItemData item;
        public int amount;
    }

    [CreateAssetMenu(fileName = "NewRecipe", menuName = "Cultivation/Recipe Data")]
    public class RecipeData : ScriptableObject
    {
        public string recipeName;
        public string description;
        public List<RecipeIngredient> inputs;
        public List<RecipeIngredient> outputs;
        public float craftingDuration;
        public MachineType requiredMachine;
        public RealmDefinition requiredRealm;
        [Range(0f, 1f)] public float successRate = 1f;
        public double qiCost;

        /// <summary>
        /// Returns true if the recipe has valid inputs and outputs.
        /// </summary>
        public bool IsValid =>
            inputs != null && inputs.Count > 0 &&
            inputs.All(i => i.item != null && i.amount > 0) &&
            outputs != null && outputs.Count > 0 &&
            outputs.All(o => o.item != null && o.amount > 0);

        /// <summary>
        /// Validates the recipe and returns a list of warning/error messages.
        /// </summary>
        public List<string> Validate()
        {
            var messages = new List<string>();

            if (inputs == null || inputs.Count == 0)
                messages.Add("Recipe has no inputs.");
            else if (inputs.Any(i => i.item == null))
                messages.Add("Recipe has input with null item.");

            if (outputs == null || outputs.Count == 0)
                messages.Add("Recipe has no outputs.");
            else if (outputs.Any(o => o.item == null))
                messages.Add("Recipe has output with null item.");

            if (craftingDuration <= 0f)
                messages.Add("craftingDuration is <= 0.");

            return messages;
        }
    }
}
