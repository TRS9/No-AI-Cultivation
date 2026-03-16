using System;
using System.Collections.Generic;
using UnityEngine;

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
        public List<RecipeIngredient> ingredients;
        public ItemData outputItem;
        public int outputAmount = 1;
        public float craftingDuration;
    }
}
