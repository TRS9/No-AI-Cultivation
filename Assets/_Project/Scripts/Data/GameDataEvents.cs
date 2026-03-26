using UnityEngine;

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

        // --- Building / Placement ---
        public delegate void MachinePlaced(MachineData machine, Vector3 position, Quaternion rotation);
        public static event MachinePlaced OnMachinePlaced;
        public static void RaiseMachinePlaced(MachineData machine, Vector3 position, Quaternion rotation)
            => OnMachinePlaced?.Invoke(machine, position, rotation);

        public delegate void BuildModeGhostStarted(MachineData machine);
        public static event BuildModeGhostStarted OnBuildModeGhostStarted;
        public static void RaiseBuildModeGhostStarted(MachineData machine)
            => OnBuildModeGhostStarted?.Invoke(machine);

        public delegate void BuildModeGhostCancelled();
        public static event BuildModeGhostCancelled OnBuildModeGhostCancelled;
        public static void RaiseBuildModeGhostCancelled()
            => OnBuildModeGhostCancelled?.Invoke();

        // --- Machine Removal ---
        public delegate void MachineRemoved(MachineData machine, UnityEngine.Vector3 position);
        public static event MachineRemoved OnMachineRemoved;
        public static void RaiseMachineRemoved(MachineData machine, UnityEngine.Vector3 position)
            => OnMachineRemoved?.Invoke(machine, position);

        // --- Machine Interaction ---
        public delegate void MachineInteracted(MonoBehaviour machine);
        public static event MachineInteracted OnMachineInteracted;
        public static void RaiseMachineInteracted(MonoBehaviour machine)
            => OnMachineInteracted?.Invoke(machine);

        public delegate void MachineProcessingCompleted(MonoBehaviour machine, RecipeData recipe);
        public static event MachineProcessingCompleted OnMachineProcessingCompleted;
        public static void RaiseMachineProcessingCompleted(MonoBehaviour machine, RecipeData recipe)
            => OnMachineProcessingCompleted?.Invoke(machine, recipe);

        // --- Spirit Pipes ---
        public delegate void PipeConnected(MonoBehaviour pipe, MonoBehaviour source, MonoBehaviour destination);
        public static event PipeConnected OnPipeConnected;
        public static void RaisePipeConnected(MonoBehaviour pipe, MonoBehaviour source, MonoBehaviour destination)
            => OnPipeConnected?.Invoke(pipe, source, destination);

        public delegate void PipeDisconnected(MonoBehaviour pipe);
        public static event PipeDisconnected OnPipeDisconnected;
        public static void RaisePipeDisconnected(MonoBehaviour pipe)
            => OnPipeDisconnected?.Invoke(pipe);

        public delegate void PipeInteracted(MonoBehaviour pipe);
        public static event PipeInteracted OnPipeInteracted;
        public static void RaisePipeInteracted(MonoBehaviour pipe)
            => OnPipeInteracted?.Invoke(pipe);

        // --- Qi Network ---
        public delegate void QiNetworkChanged(float totalDemand, float available);
        public static event QiNetworkChanged OnQiNetworkChanged;
        public static void RaiseQiNetworkChanged(float demand, float available)
            => OnQiNetworkChanged?.Invoke(demand, available);

        // --- Resource Extraction ---
        public delegate void ResourceExtracted(ItemData resource, int amount);
        public static event ResourceExtracted OnResourceExtracted;
        public static void RaiseResourceExtracted(ItemData resource, int amount)
            => OnResourceExtracted?.Invoke(resource, amount);

        // --- Combat / Enemy (Phase 8) ---
        public delegate void EnemyDamaged(MonoBehaviour enemy, float damage);
        public static event EnemyDamaged OnEnemyDamaged;
        public static void RaiseEnemyDamaged(MonoBehaviour enemy, float damage)
            => OnEnemyDamaged?.Invoke(enemy, damage);

        public delegate void EnemyDied(MonoBehaviour enemy);
        public static event EnemyDied OnEnemyDied;
        public static void RaiseEnemyDied(MonoBehaviour enemy)
            => OnEnemyDied?.Invoke(enemy);

        public delegate void EnemySpawned(MonoBehaviour enemy);
        public static event EnemySpawned OnEnemySpawned;
        public static void RaiseEnemySpawned(MonoBehaviour enemy)
            => OnEnemySpawned?.Invoke(enemy);

        public delegate void LootDropped(ItemData item, int amount, UnityEngine.Vector3 position);
        public static event LootDropped OnLootDropped;
        public static void RaiseLootDropped(ItemData item, int amount, UnityEngine.Vector3 position)
            => OnLootDropped?.Invoke(item, amount, position);
    }
}
