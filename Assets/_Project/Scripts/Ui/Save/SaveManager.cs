using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Player;
using CultivationGame.Systems;

namespace CultivationGame.UI
{
    [DefaultExecutionOrder(-100)]
    public class SaveManager : MonoBehaviour
    {
        [Header("Player References")]
        public PlayerStats playerStats;
        public PlayerInventory playerInventory;
        public Transform playerTransform;
        public Rigidbody playerRigidbody;

        [Header("Data References")]
        public List<RealmDefinition> allRealms;
        public List<ItemData> allItems;

        [Header("Machine References")]
        [Tooltip("All MachineData assets in the project. Used to look up machines by name when loading.")]
        public List<MachineData> allMachines;
        [Tooltip("The build grid used to track occupied cells.")]
        public BuildGrid buildGrid;
        [Tooltip("Recipe database for restoring machine recipes on load.")]
        public RecipeDatabase recipeDatabase;
        [Tooltip("Layer mask that placed machines should be assigned to (must match PlayerInteractor's interactableLayer).")]
        [SerializeField] private LayerMask machineLayer;

        public static SaveManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            Load();
        }

        private void OnApplicationQuit() => Save();

        public void Save()
        {
            if (playerStats == null || playerInventory == null || playerTransform == null) return;

            var data = new SaveData
            {
                currentQi = playerStats.currentQi,
                currentRealmIndex = playerStats.currentRealm?.realmIndex ?? 0,
                positionX = playerTransform.position.x,
                positionY = playerTransform.position.y,
                positionZ = playerTransform.position.z,
                rotationY = playerTransform.eulerAngles.y
            };

            foreach (var kvp in playerInventory.GetItems())
                data.inventoryEntries.Add(new InventorySaveEntry { essenceId = kvp.Key.ItemId, count = kvp.Value });

            // World state
            data.collectedEssenceIds = new List<string>(WorldState.CollectedIds);
            foreach (var kv in WorldState.SpawnerTimestamps)
                data.spawnerEntries.Add(new SpawnerSaveEntry { spawnerId = kv.Key, collectedAtTicks = kv.Value });

            // Scene persistence
            data.currentScene = SceneManager.GetActiveScene().name;
            if (SceneTransitionData.HasPendingReturn)
            {
                data.returnScene = SceneTransitionData.ReturnScene;
                data.returnPositionX = SceneTransitionData.ReturnPosition.x;
                data.returnPositionY = SceneTransitionData.ReturnPosition.y;
                data.returnPositionZ = SceneTransitionData.ReturnPosition.z;
                data.returnRotationY = SceneTransitionData.ReturnRotationY;
            }

            // Minor Realm — persist biome + seed so the same world can be regenerated on load
            if (SceneTransitionData.IsMinorRealm)
            {
                data.realmBiome = SceneTransitionData.RealmBiome.ToString();
                data.realmSeed  = SceneTransitionData.RealmSeed;
            }

            // Placed machines
            SaveMachines(data);

            SaveSystem.SaveGame(data);
        }

        public void Load()
        {
            var data = SaveSystem.LoadGame();
            if (data == null) return;

            // Clear stale static data only after confirming a save file exists,
            // otherwise a missing save wipes the current world state for nothing.
            WorldState.Clear();
            foreach (var id in data.collectedEssenceIds)
                WorldState.CollectedIds.Add(id);
            foreach (var e in data.spawnerEntries)
                WorldState.SpawnerTimestamps[e.spawnerId] = e.collectedAtTicks;

            // Redirect to the scene that was active when the game was saved
            var savedScene = string.IsNullOrEmpty(data.currentScene)
                ? SceneManager.GetActiveScene().name : data.currentScene;
            if (savedScene != SceneManager.GetActiveScene().name)
            {
                // Restore return point so the exit portal knows where to go
                if (!string.IsNullOrEmpty(data.returnScene))
                    SceneTransitionData.SetReturn(data.returnScene,
                        new Vector3(data.returnPositionX, data.returnPositionY, data.returnPositionZ),
                        data.returnRotationY);

                // If the saved scene is a Minor Realm, restore biome + seed so
                // MinorRealmGenerator recreates the exact same world.
                if (!string.IsNullOrEmpty(data.realmBiome) &&
                    System.Enum.TryParse<CultivationGame.Core.BiomeType>(data.realmBiome, out var biome))
                    SceneTransitionData.SetRealm(biome, data.realmSeed);

                SceneManager.LoadScene(savedScene);
                return; // Target scene's SaveManager will apply player state
            }

            if (playerStats == null || playerInventory == null || playerTransform == null) return;

            // Restore realm
            var realm = allRealms?.Find(r => r.realmIndex == data.currentRealmIndex);
            if (realm != null) playerStats.currentRealm = realm;
            playerStats.currentQi = data.currentQi;
            GameEvents.RaiseQiChanged(playerStats.currentQi, playerStats.MaxQi);
            GameEvents.RaiseRealmChanged(playerStats.RealmName, playerStats.SubStage);

            // Restore position — must set rb.position directly so the physics world
            // matches the transform; otherwise the Rigidbody overrides the teleport.
            var pos = new Vector3(data.positionX, data.positionY, data.positionZ);
            playerTransform.position = pos;
            playerTransform.eulerAngles = new Vector3(0f, data.rotationY, 0f);
            if (playerRigidbody != null)
            {
                playerRigidbody.position = pos;
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            // Restore inventory
            var loaded = new Dictionary<ItemData, int>();
            foreach (var entry in data.inventoryEntries)
            {
                var item = allItems?.Find(i => i.ItemId == entry.essenceId);
                if (item != null) loaded[item] = entry.count;
            }
            playerInventory.LoadInventory(loaded);

            // Restore placed machines
            LoadMachines(data);
        }

        // ------------------------------------------------------------------ //
        //  Machine Save
        // ------------------------------------------------------------------ //

        private void SaveMachines(SaveData data)
        {
            SaveMachinesOfType<BaseMachine>(data);
            SaveMachinesOfType<ResourceExtractor>(data);
            SaveMachinesOfType<StorageContainer>(data);
            SaveMachinesOfType<QiConduit>(data);
            SaveMachinesOfType<Splitter>(data);
            SaveMachinesOfType<Merger>(data);
        }

        private void SaveMachinesOfType<T>(SaveData data) where T : MonoBehaviour, IMachineConnectable
        {
#if UNITY_2023_1_OR_NEWER
            var machines = FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            var machines = FindObjectsOfType<T>();
#endif
            foreach (var machine in machines)
            {
                var md = machine.MachineData;
                if (md == null) continue;

                int rot = Mathf.RoundToInt(machine.transform.eulerAngles.y / 90f) % 4;
                data.buildingEntries.Add(new BuildingSaveEntry
                {
                    machineId = md.name,
                    posX = machine.transform.position.x,
                    posY = machine.transform.position.y,
                    posZ = machine.transform.position.z,
                    rotation = rot
                });

                SaveMachineInventory(data, machine, machine);
            }
        }

        private void SaveMachineInventory(SaveData data, IMachineConnectable connectable, MonoBehaviour mb)
        {
            var input = connectable.InputInventory;
            var output = connectable.OutputInventory;
            string recipeId = (connectable is BaseMachine bm && bm.CurrentRecipe != null)
                ? bm.CurrentRecipe.name : null;

            bool hasContent = recipeId != null
                || (input != null && input.TotalCount() > 0)
                || (output != null && output != input && output.TotalCount() > 0);

            if (!hasContent) return;

            var entry = new MachineInventorySaveEntry
            {
                machinePosX = mb.transform.position.x,
                machinePosY = mb.transform.position.y,
                machinePosZ = mb.transform.position.z,
                recipeId = recipeId
            };

            if (input != null)
                foreach (var kvp in input.GetSnapshot())
                    entry.inputItems.Add(new InventorySaveEntry { essenceId = kvp.Key.ItemId, count = kvp.Value });

            if (output != null && output != input)
                foreach (var kvp in output.GetSnapshot())
                    entry.outputItems.Add(new InventorySaveEntry { essenceId = kvp.Key.ItemId, count = kvp.Value });

            data.machineInventories.Add(entry);
        }

        // ------------------------------------------------------------------ //
        //  Machine Load
        // ------------------------------------------------------------------ //

        private void LoadMachines(SaveData data)
        {
            if (data.buildingEntries == null || data.buildingEntries.Count == 0) return;
            if (allMachines == null) return;

            foreach (var entry in data.buildingEntries)
            {
                var md = allMachines.Find(m => m.name == entry.machineId);
                if (md == null || md.prefab == null)
                {
                    Debug.LogWarning($"[SaveManager] Machine data '{entry.machineId}' not found — skipping.");
                    continue;
                }

                Vector3 position = new Vector3(entry.posX, entry.posY, entry.posZ);
                Quaternion rotation = Quaternion.Euler(0f, entry.rotation * 90f, 0f);

                GameObject placed = Instantiate(md.prefab, position, rotation);
                placed.name = md.machineName;

                // Wire machine data
                WireMachineData(placed, md);

                // Set layer for interaction and removal detection
                SetLayerRecursive(placed, GetLayerFromMask(machineLayer));

                // Occupy grid cells
                if (buildGrid != null)
                    buildGrid.OccupyCells(position, md.gridSize, entry.rotation);
            }

            // Restore inventories after all machines have been instantiated
            RestoreMachineInventories(data);
        }

        private void RestoreMachineInventories(SaveData data)
        {
            if (data.machineInventories == null || data.machineInventories.Count == 0) return;

            foreach (var invEntry in data.machineInventories)
            {
                Vector3 pos = new Vector3(invEntry.machinePosX, invEntry.machinePosY, invEntry.machinePosZ);
                var connectable = FindMachineAtPosition(pos);
                if (connectable == null) continue;

                // Restore recipe
                if (!string.IsNullOrEmpty(invEntry.recipeId) && connectable is BaseMachine bm)
                {
                    var recipe = FindRecipeByName(invEntry.recipeId);
                    if (recipe != null) bm.SetRecipe(recipe);
                }

                // Restore input inventory
                if (connectable.InputInventory != null && invEntry.inputItems != null && invEntry.inputItems.Count > 0)
                {
                    var items = ResolveItems(invEntry.inputItems);
                    connectable.InputInventory.LoadFrom(items);
                }

                // Restore output inventory (skip if same as input, e.g. StorageContainer)
                if (connectable.OutputInventory != null && connectable.OutputInventory != connectable.InputInventory
                    && invEntry.outputItems != null && invEntry.outputItems.Count > 0)
                {
                    var items = ResolveItems(invEntry.outputItems);
                    connectable.OutputInventory.LoadFrom(items);
                }
            }
        }

        // ------------------------------------------------------------------ //
        //  Helpers
        // ------------------------------------------------------------------ //

        private static void WireMachineData(GameObject placed, MachineData data)
        {
            if (placed.GetComponent<BaseMachine>() is BaseMachine bm)
                bm.SetMachineData(data);
            else if (placed.GetComponent<ResourceExtractor>() is ResourceExtractor ext)
                ext.SetMachineData(data);
            else if (placed.GetComponent<StorageContainer>() is StorageContainer sc)
                sc.SetMachineData(data);
            else if (placed.GetComponent<QiConduit>() is QiConduit conduit)
                conduit.SetMachineData(data);
            else if (placed.GetComponent<Splitter>() is Splitter splitter)
                splitter.SetMachineData(data);
            else if (placed.GetComponent<Merger>() is Merger merger)
                merger.SetMachineData(data);
        }

        private IMachineConnectable FindMachineAtPosition(Vector3 pos)
        {
            const float tolerance = 0.1f;
            IMachineConnectable result;
            if ((result = FindAtPos<BaseMachine>(pos, tolerance)) != null) return result;
            if ((result = FindAtPos<ResourceExtractor>(pos, tolerance)) != null) return result;
            if ((result = FindAtPos<StorageContainer>(pos, tolerance)) != null) return result;
            if ((result = FindAtPos<QiConduit>(pos, tolerance)) != null) return result;
            if ((result = FindAtPos<Splitter>(pos, tolerance)) != null) return result;
            if ((result = FindAtPos<Merger>(pos, tolerance)) != null) return result;
            return null;
        }

        private static T FindAtPos<T>(Vector3 pos, float tolerance) where T : MonoBehaviour, IMachineConnectable
        {
#if UNITY_2023_1_OR_NEWER
            var machines = FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            var machines = FindObjectsOfType<T>();
#endif
            foreach (var m in machines)
                if (Vector3.Distance(m.transform.position, pos) < tolerance) return m;
            return null;
        }

        private RecipeData FindRecipeByName(string recipeName)
        {
            if (recipeDatabase == null || recipeDatabase.allRecipes == null) return null;
            return recipeDatabase.allRecipes.Find(r => r.name == recipeName);
        }

        private Dictionary<ItemData, int> ResolveItems(List<InventorySaveEntry> entries)
        {
            var result = new Dictionary<ItemData, int>();
            foreach (var entry in entries)
            {
                var item = allItems?.Find(i => i.ItemId == entry.essenceId);
                if (item != null) result[item] = entry.count;
            }
            return result;
        }

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        private static int GetLayerFromMask(LayerMask mask)
        {
            int value = mask.value;
            for (int i = 0; i < 32; i++)
                if ((value & (1 << i)) != 0) return i;
            return 0;
        }
    }
}
