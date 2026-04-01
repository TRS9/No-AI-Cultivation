using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
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

        private int _interactableLayerIndex;

        public static SaveManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;

            _interactableLayerIndex = LayerMask.NameToLayer("Interactable");
            if (_interactableLayerIndex < 0)
            {
                Debug.LogWarning("[SaveManager] 'Interactable' layer not found \u2014 falling back to machineLayer.");
                _interactableLayerIndex = LayerHelper.GetLayerFromMask(machineLayer);
            }

            Load();
        }

        private void OnApplicationQuit() => Save();

        public void Save()
        {
            var data = new SaveData();

            if (playerStats != null && playerInventory != null && playerTransform != null)
            {
                data.currentQi = playerStats.currentQi;
                data.currentRealmIndex = playerStats.currentRealm?.realmIndex ?? 0;
                data.positionX = playerTransform.position.x;
                data.positionY = playerTransform.position.y;
                data.positionZ = playerTransform.position.z;
                data.rotationY = playerTransform.eulerAngles.y;

                foreach (var kvp in playerInventory.GetItems())
                    data.inventoryEntries.Add(new InventorySaveEntry { essenceId = kvp.Key.ItemId, count = kvp.Value });
            }
            else
            {
                Debug.LogWarning("[SaveManager] Player references missing \u2014 saving machines only.");
            }

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

            Debug.Log($"[SaveManager] Saving {data.buildingEntries.Count} machines, {data.inventoryEntries.Count} inventory items.");

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

            // Restore placed machines (independent of player references)
            LoadMachines(data);

            if (playerStats == null || playerInventory == null || playerTransform == null)
            {
                Debug.LogWarning("[SaveManager] Player references missing — skipping player state restoration.");
                return;
            }

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
            SaveSpiritPipes(data);
            SaveOreVeins(data);
        }

        private void SaveMachinesOfType<T>(SaveData data) where T : MonoBehaviour, IMachineConnectable
        {
#if UNITY_2023_1_OR_NEWER
            var machines = FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            var machines = FindObjectsOfType<T>();
#endif
            Debug.Log($"[SaveManager] FindObjectsByType<{typeof(T).Name}>() found {machines.Length} instances.");
            foreach (var machine in machines)
            {
                var md = machine.MachineData;
                if (md == null)
                {
                    Debug.LogWarning($"[SaveManager] Skipping {typeof(T).Name} '{machine.name}' at {machine.transform.position} — MachineData is null.");
                    continue;
                }

                var guidComp = machine.GetComponent<MachineGuid>();
                string guid = guidComp != null ? guidComp.Guid : null;
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning($"[SaveManager] Machine '{md.name}' at {machine.transform.position} has no GUID — assigning one now.");
                    if (guidComp == null) guidComp = machine.gameObject.AddComponent<MachineGuid>();
                    guidComp.AssignNewGuid();
                    guid = guidComp.Guid;
                }

                int rot = Mathf.RoundToInt(machine.transform.eulerAngles.y / 90f) % 4;
                data.buildingEntries.Add(new BuildingSaveEntry
                {
                    guid = guid,
                    machineId = md.name,
                    posX = machine.transform.position.x,
                    posY = machine.transform.position.y,
                    posZ = machine.transform.position.z,
                    rotation = rot
                });

                SaveMachineInventory(data, machine, guid);
            }
        }

        private void SaveSpiritPipes(SaveData data)
        {
#if UNITY_2023_1_OR_NEWER
            var pipes = FindObjectsByType<SpiritPipe>(FindObjectsSortMode.None);
#else
            var pipes = FindObjectsOfType<SpiritPipe>();
#endif
            foreach (var pipe in pipes)
            {
                var md = pipe.MachineData;
                if (md == null) continue;

                var guidComp = pipe.GetComponent<MachineGuid>();
                string guid = guidComp != null ? guidComp.Guid : null;
                if (string.IsNullOrEmpty(guid))
                {
                    if (guidComp == null) guidComp = pipe.gameObject.AddComponent<MachineGuid>();
                    guidComp.AssignNewGuid();
                    guid = guidComp.Guid;
                }

                int rot = Mathf.RoundToInt(pipe.transform.eulerAngles.y / 90f) % 4;
                data.buildingEntries.Add(new BuildingSaveEntry
                {
                    guid = guid,
                    machineId = md.name,
                    posX = pipe.transform.position.x,
                    posY = pipe.transform.position.y,
                    posZ = pipe.transform.position.z,
                    rotation = rot
                });

                // Save connection state
                if (!pipe.IsConnected) continue;
                var srcMb = pipe.Source as MonoBehaviour;
                var dstMb = pipe.Destination as MonoBehaviour;
                if (srcMb == null || dstMb == null) continue;

                string sourceGuid = srcMb.GetComponent<MachineGuid>()?.Guid;
                string destGuid = dstMb.GetComponent<MachineGuid>()?.Guid;
                if (string.IsNullOrEmpty(sourceGuid) || string.IsNullOrEmpty(destGuid)) continue;

                data.pipeConnections.Add(new PipeConnectionSaveEntry
                {
                    pipeGuid = guid,
                    sourceGuid = sourceGuid,
                    destGuid = destGuid,
                    filterItemId = pipe.FilterItem != null ? pipe.FilterItem.ItemId : null
                });
            }
        }

        private void SaveOreVeins(SaveData data)
        {
#if UNITY_2023_1_OR_NEWER
            var veins = FindObjectsByType<OreVein>(FindObjectsSortMode.None);
#else
            var veins = FindObjectsOfType<OreVein>();
#endif
            foreach (var vein in veins)
            {
                if (string.IsNullOrEmpty(vein.UniqueId)) continue;
                data.oreVeinEntries.Add(new OreVeinSaveEntry
                {
                    veinId = vein.UniqueId,
                    remainingYield = vein.RemainingYield
                });
            }
        }

        private void SaveMachineInventory(SaveData data, IMachineConnectable connectable, string machineGuid)
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
                machineGuid = machineGuid,
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
            if (data.buildingEntries == null || data.buildingEntries.Count == 0)
            {
                Debug.Log("[SaveManager] No machines to load.");
                RestoreOreVeins(data);
                return;
            }
            if (allMachines == null || allMachines.Count == 0)
            {
                Debug.LogWarning("[SaveManager] allMachines list is empty — cannot load machines. Assign MachineData assets in the inspector.");
                RestoreOreVeins(data);
                return;
            }

            Debug.Log($"[SaveManager] Loading {data.buildingEntries.Count} machines...");

            // Build GUID → machine lookups for inventory + pipe connection restoration
            var machinesByGuid = new Dictionary<string, IMachineConnectable>();
            var pipesByGuid = new Dictionary<string, SpiritPipe>();

            foreach (var entry in data.buildingEntries)
            {
                var md = allMachines.Find(m => m.name == entry.machineId);
                if (md == null || md.prefab == null)
                {
                    Debug.LogWarning($"[SaveManager] Machine data '{entry.machineId}' not found — skipping.");
                    continue;
                }

                // Backward compat: generate GUID for old saves that don't have one
                string guid = entry.guid;
                if (string.IsNullOrEmpty(guid))
                    guid = System.Guid.NewGuid().ToString();

                Vector3 position = new Vector3(entry.posX, entry.posY, entry.posZ);
                Quaternion rotation = Quaternion.Euler(0f, entry.rotation * 90f, 0f);

                GameObject placed = Instantiate(md.prefab, position, rotation);
                placed.name = md.machineName;

                // Restore persistent GUID
                var guidComp = placed.AddComponent<MachineGuid>();
                guidComp.SetGuid(guid);

                // Wire machine data
                WireMachineData(placed, md);

                // Set layer for interaction and removal detection
                LayerHelper.SetLayerRecursive(placed, _interactableLayerIndex);

                // Occupy grid cells
                if (buildGrid != null)
                    buildGrid.OccupyCells(position, md.gridSize, entry.rotation);

                // Register for inventory restoration
                var connectable = placed.GetComponent<IMachineConnectable>();
                if (connectable != null)
                    machinesByGuid[guid] = connectable;

                // Track pipes separately for connection restoration
                var pipe = placed.GetComponent<SpiritPipe>();
                if (pipe != null)
                    pipesByGuid[guid] = pipe;
            }

            // Restore inventories after all machines have been instantiated
            RestoreMachineInventories(data, machinesByGuid);

            // Restore pipe connections
            RestorePipeConnections(data, pipesByGuid, machinesByGuid);

            // Restore ore vein yields
            RestoreOreVeins(data);
        }

        private void RestorePipeConnections(SaveData data,
            Dictionary<string, SpiritPipe> pipesByGuid,
            Dictionary<string, IMachineConnectable> machinesByGuid)
        {
            if (data.pipeConnections == null || data.pipeConnections.Count == 0) return;

            foreach (var conn in data.pipeConnections)
            {
                if (string.IsNullOrEmpty(conn.pipeGuid)) continue;
                if (!pipesByGuid.TryGetValue(conn.pipeGuid, out var pipe)) continue;

                machinesByGuid.TryGetValue(conn.sourceGuid ?? "", out var source);
                machinesByGuid.TryGetValue(conn.destGuid ?? "", out var dest);

                if (source != null && dest != null)
                {
                    pipe.Connect(source, dest);

                    // Restore filter
                    if (!string.IsNullOrEmpty(conn.filterItemId))
                    {
                        var filterItem = allItems?.Find(i => i.ItemId == conn.filterItemId);
                        if (filterItem != null) pipe.SetFilter(filterItem);
                    }
                }
                else
                {
                    Debug.LogWarning($"[SaveManager] Pipe connection missing source or dest — pipe:{conn.pipeGuid} src:{conn.sourceGuid} dst:{conn.destGuid}");
                }
            }
        }

        private void RestoreOreVeins(SaveData data)
        {
            if (data.oreVeinEntries == null || data.oreVeinEntries.Count == 0) return;

            var yieldByVeinId = new Dictionary<string, int>();
            foreach (var entry in data.oreVeinEntries)
                yieldByVeinId[entry.veinId] = entry.remainingYield;

#if UNITY_2023_1_OR_NEWER
            var veins = FindObjectsByType<OreVein>(FindObjectsSortMode.None);
#else
            var veins = FindObjectsOfType<OreVein>();
#endif
            foreach (var vein in veins)
            {
                if (string.IsNullOrEmpty(vein.UniqueId)) continue;
                if (yieldByVeinId.TryGetValue(vein.UniqueId, out int yield))
                    vein.LoadRemainingYield(yield);
            }
        }

        private void RestoreMachineInventories(SaveData data, Dictionary<string, IMachineConnectable> machinesByGuid)
        {
            if (data.machineInventories == null || data.machineInventories.Count == 0) return;

            foreach (var invEntry in data.machineInventories)
            {
                if (string.IsNullOrEmpty(invEntry.machineGuid)) continue;
                if (!machinesByGuid.TryGetValue(invEntry.machineGuid, out var connectable)) continue;

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
            else if (placed.GetComponent<SpiritPipe>() is SpiritPipe pipe)
                pipe.SetMachineData(data);
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
    }
}
