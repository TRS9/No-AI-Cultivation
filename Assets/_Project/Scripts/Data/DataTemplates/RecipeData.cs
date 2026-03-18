using System;
using System.Collections.Generic;
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
    }
}
