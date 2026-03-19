namespace CultivationGame.Data
{
    public static class GameDataEvents
    {
        // --- Crafting ---
        public delegate void CraftingStarted(RecipeData recipe);
        public static event CraftingStarted OnCraftingStarted;
        public static void RaiseCraftingStarted(RecipeData recipe)
            => OnCraftingStarted?.Invoke(recipe);

        public delegate void CraftingCompleted(RecipeData recipe);
        public static event CraftingCompleted OnCraftingCompleted;
        public static void RaiseCraftingCompleted(RecipeData recipe)
            => OnCraftingCompleted?.Invoke(recipe);

        public delegate void CraftingFailed(RecipeData recipe);
        public static event CraftingFailed OnCraftingFailed;
        public static void RaiseCraftingFailed(RecipeData recipe)
            => OnCraftingFailed?.Invoke(recipe);

        // --- Crafting Progress ---
        public delegate void CraftingProgressChanged(RecipeData recipe, float normalizedProgress);
        public static event CraftingProgressChanged OnCraftingProgressChanged;
        public static void RaiseCraftingProgressChanged(RecipeData recipe, float progress)
            => OnCraftingProgressChanged?.Invoke(recipe, progress);

        // --- Pills ---
        public delegate void PillConsumed(PillData pill);
        public static event PillConsumed OnPillConsumed;
        public static void RaisePillConsumed(PillData pill)
            => OnPillConsumed?.Invoke(pill);

        public delegate void PillEffectsApplied(PillData pill, float effectiveness);
        public static event PillEffectsApplied OnPillEffectsApplied;
        public static void RaisePillEffectsApplied(PillData pill, float effectiveness)
            => OnPillEffectsApplied?.Invoke(pill, effectiveness);
    }
}
