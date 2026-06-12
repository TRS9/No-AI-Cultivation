using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Player;
using CultivationGame.Systems;

namespace CultivationGame.UI
{
    /// <summary>
    /// Persistent save/load coordinator.
    ///
    /// Lives across scene loads (DontDestroyOnLoad) so saving works from every
    /// scene, quitting anywhere autosaves, and portal transitions no longer
    /// re-read the save file (which used to roll back unsaved progress).
    ///
    /// Machines are tagged with a scene key ("Universe", "Grotto",
    /// "MinorRealm#&lt;seed&gt;") so each scene only saves/restores its own
    /// buildings while entries from other scenes are preserved in the file.
    /// </summary>
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
        [Tooltip("The build grid used to track occupied cells. Re-resolved per scene.")]
        public BuildGrid buildGrid;
        [Tooltip("Recipe database for restoring machine recipes on load.")]
        public RecipeDatabase recipeDatabase;
        [Tooltip("Layer mask that placed machines should be assigned to (must match PlayerInteractor's interactableLayer).")]
        [SerializeField] private LayerMask machineLayer;

        [Header("Scenes")]
        [Tooltip("Scene loaded when starting a new game.")]
        [SerializeField] private string mainSceneName = "Universe";

        private int _interactableLayerIndex;
        private SaveData _data;               // in-memory save state (carries all scenes)
        private bool _pendingPlayerRestore;   // apply player block on next scene restore

        public static SaveManager Instance { get; private set; }

        // ------------------------------------------------------------------ //
        //  Lifecycle
        // ------------------------------------------------------------------ //

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _interactableLayerIndex = LayerMask.NameToLayer("Interactable");
            if (_interactableLayerIndex < 0)
            {
                Debug.LogWarning("[SaveManager] 'Interactable' layer not found — falling back to machineLayer.");
                _interactableLayerIndex = LayerHelper.GetLayerFromMask(machineLayer);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;

            _data = SaveSystem.LoadGame();
            if (_data == null) return;

            ApplyWorldState(_data);
            _pendingPlayerRestore = true;

            // Redirect to the scene that was active when the game was saved.
            // Scene restoration happens in OnSceneLoaded (fires for the initial
            // scene as well, since we subscribed during Awake).
            string savedScene = string.IsNullOrEmpty(_data.currentScene)
                ? SceneManager.GetActiveScene().name : _data.currentScene;
            if (savedScene != SceneManager.GetActiveScene().name)
            {
                PrepareSceneTransition(_data);
                SceneManager.LoadScene(savedScene);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void OnApplicationQuit() => Save();

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshSceneRefs();
            if (_data == null) return;
            RestoreCurrentScene();
        }

        /// <summary>
        /// Re-resolves references that are scene-local (build grid) or may have
        /// been replaced (player after New Game).
        /// </summary>
        private void RefreshSceneRefs()
        {
            if (buildGrid == null) buildGrid = FindFirstObjectByType<BuildGrid>();

            if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                if (playerInventory == null) playerInventory = playerStats.GetComponent<PlayerInventory>();
                if (playerTransform == null) playerTransform = playerStats.transform;
                if (playerRigidbody == null) playerRigidbody = playerStats.GetComponent<Rigidbody>();
            }
        }

        /// <summary>
        /// Scene identity used to partition machine save entries. Minor realms are
        /// keyed by seed so each generated world keeps its own buildings.
        /// </summary>
        private static string CurrentSceneKey()
        {
            string name = SceneManager.GetActiveScene().name;
            return SceneTransitionData.IsMinorRealm
                ? $"{name}#{SceneTransitionData.RealmSeed}"
                : name;
        }

        // Legacy saves predate scene keys; all of their machines were saved in the main scene.
        private string NormalizeKey(string key)
            => string.IsNullOrEmpty(key) ? mainSceneName : key;

        // ------------------------------------------------------------------ //
        //  Save
        // ------------------------------------------------------------------ //

        public void Save()
        {
            if (_data == null) _data = new SaveData();

            if (playerStats != null && playerInventory != null && playerTransform != null)
            {
                _data.currentQi = playerStats.currentQi;
                _data.currentRealmIndex = playerStats.currentRealm?.realmIndex ?? 0;
                _data.positionX = playerTransform.position.x;
                _data.positionY = playerTransform.position.y;
                _data.positionZ = playerTransform.position.z;
                _data.rotationY = playerTransform.eulerAngles.y;

                _data.inventoryEntries.Clear();
                foreach (var kvp in playerInventory.GetItems())
                    _data.inventoryEntries.Add(new InventorySaveEntry { essenceId = kvp.Key.ItemId, count = kvp.Value });
            }
            else
            {
                Debug.LogWarning("[SaveManager] Player references missing — saving machines only.");
            }

            // World state
            _data.collectedEssenceIds = new List<string>(WorldState.CollectedIds);
            _data.spawnerEntries.Clear();
            foreach (var kv in WorldState.SpawnerTimestamps)
                _data.spawnerEntries.Add(new SpawnerSaveEntry { spawnerId = kv.Key, collectedAtTicks = kv.Value });

            // Scene persistence
            _data.currentScene = SceneManager.GetActiveScene().name;
            if (SceneTransitionData.HasPendingReturn)
            {
                _data.returnScene = SceneTransitionData.ReturnScene;
                _data.returnPositionX = SceneTransitionData.ReturnPosition.x;
                _data.returnPositionY = SceneTransitionData.ReturnPosition.y;
                _data.returnPositionZ = SceneTransitionData.ReturnPosition.z;
                _data.returnRotationY = SceneTransitionData.ReturnRotationY;
            }

            // Minor Realm — persist biome + seed so the same world can be regenerated on load
            if (SceneTransitionData.IsMinorRealm)
            {
                _data.realmBiome = SceneTransitionData.RealmBiome.ToString();
                _data.realmSeed  = SceneTransitionData.RealmSeed;
            }
            else
            {
                _data.realmBiome = null;
                _data.realmSeed = 0;
            }

            // Placed machines — replace this scene's entries, keep all other scenes'
            MergeMachineEntries();

            SaveSystem.SaveGame(_data);
        }

        /// <summary>
        /// Replaces the current scene's machine/pipe/inventory entries with a fresh
        /// scan while preserving entries belonging to other scenes.
        /// </summary>
        private void MergeMachineEntries()
        {
            string key = CurrentSceneKey();

            var removedGuids = new HashSet<string>();
            _data.buildingEntries.RemoveAll(e =>
            {
                if (NormalizeKey(e.sceneKey) != key) return false;
                removedGuids.Add(e.guid);
                return true;
            });
            _data.pipeConnections.RemoveAll(p => removedGuids.Contains(p.pipeGuid));
            _data.machineInventories.RemoveAll(m => removedGuids.Contains(m.machineGuid));

            SaveMachinesOfType<BaseMachine>(_data, key);
            SaveMachinesOfType<ResourceExtractor>(_data, key);
            SaveMachinesOfType<StorageContainer>(_data, key);
            SaveMachinesOfType<QiConduit>(_data, key);
            SaveMachinesOfType<Splitter>(_data, key);
            SaveMachinesOfType<Merger>(_data, key);
            SaveSpiritPipes(_data, key);
            SaveOreVeins(_data);
        }

        private void SaveMachinesOfType<T>(SaveData data, string sceneKey) where T : MonoBehaviour, IMachineConnectable
        {
            var machines = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (var machine in machines)
            {
                var md = machine.MachineData;
                if (md == null)
                {
                    Debug.LogWarning($"[SaveManager] Skipping {typeof(T).Name} '{machine.name}' at {machine.transform.position} — MachineData is null.");
                    continue;
                }

                string guid = EnsureGuid(machine.gameObject, md.name);

                int rot = Mathf.RoundToInt(machine.transform.eulerAngles.y / 90f) % 4;
                data.buildingEntries.Add(new BuildingSaveEntry
                {
                    guid = guid,
                    machineId = md.name,
                    posX = machine.transform.position.x,
                    posY = machine.transform.position.y,
                    posZ = machine.transform.position.z,
                    rotation = rot,
                    sceneKey = sceneKey
                });

                SaveMachineInventory(data, machine, guid);
            }
        }

        private void SaveSpiritPipes(SaveData data, string sceneKey)
        {
            var pipes = FindObjectsByType<SpiritPipe>(FindObjectsSortMode.None);
            foreach (var pipe in pipes)
            {
                var md = pipe.MachineData;
                if (md == null) continue;

                string guid = EnsureGuid(pipe.gameObject, md.name);

                int rot = Mathf.RoundToInt(pipe.transform.eulerAngles.y / 90f) % 4;
                data.buildingEntries.Add(new BuildingSaveEntry
                {
                    guid = guid,
                    machineId = md.name,
                    posX = pipe.transform.position.x,
                    posY = pipe.transform.position.y,
                    posZ = pipe.transform.position.z,
                    rotation = rot,
                    sceneKey = sceneKey
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

        private static string EnsureGuid(GameObject machineObject, string machineName)
        {
            var guidComp = machineObject.GetComponent<MachineGuid>();
            if (guidComp == null) guidComp = machineObject.AddComponent<MachineGuid>();
            if (string.IsNullOrEmpty(guidComp.Guid))
            {
                Debug.LogWarning($"[SaveManager] Machine '{machineName}' had no GUID — assigning one now.");
                guidComp.AssignNewGuid();
            }
            return guidComp.Guid;
        }

        private void SaveOreVeins(SaveData data)
        {
            // Vein IDs are globally unique — update entries for veins present in this
            // scene, keep the rest (they belong to other scenes / other realm seeds).
            var veins = FindObjectsByType<OreVein>(FindObjectsSortMode.None);
            var sceneVeinIds = new HashSet<string>();
            foreach (var vein in veins)
                if (!string.IsNullOrEmpty(vein.UniqueId))
                    sceneVeinIds.Add(vein.UniqueId);

            data.oreVeinEntries.RemoveAll(e => sceneVeinIds.Contains(e.veinId));

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
        //  Load
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Re-reads the save file mid-session (pause menu "Load"). Existing placed
        /// machines are removed before restoring so nothing is duplicated.
        /// </summary>
        public void Load()
        {
            var fresh = SaveSystem.LoadGame();
            if (fresh == null) return;

            _data = fresh;
            ApplyWorldState(_data);
            _pendingPlayerRestore = true;

            string savedScene = string.IsNullOrEmpty(_data.currentScene)
                ? SceneManager.GetActiveScene().name : _data.currentScene;
            if (savedScene != SceneManager.GetActiveScene().name)
            {
                PrepareSceneTransition(_data);
                SceneManager.LoadScene(savedScene);
                return; // OnSceneLoaded restores the target scene
            }

            // Same scene: clear existing machines, then restore from the file.
            foreach (var guidComp in FindObjectsByType<MachineGuid>(FindObjectsSortMode.None))
                DestroyImmediate(guidComp.gameObject);
            if (buildGrid != null) buildGrid.ClearAllCells();

            RestoreCurrentScene();
        }

        /// <summary>
        /// Deletes the save and restarts with a genuinely clean state: persistent
        /// player destroyed, static world/buff/transition state reset.
        /// </summary>
        public void NewGame()
        {
            SaveSystem.DeleteSave();
            _data = null;
            _pendingPlayerRestore = false;

            WorldState.Clear();
            CultivationBuffs.ResetAll();
            SceneTransitionData.ResetAll();
            Time.timeScale = 1f;

            // The persistent player carries qi/realm/inventory — it must go so the
            // freshly loaded scene's player (with default values) takes over.
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) Destroy(player);
            playerStats = null;
            playerInventory = null;
            playerTransform = null;
            playerRigidbody = null;

            SceneManager.LoadScene(mainSceneName);
        }

        private void ApplyWorldState(SaveData data)
        {
            WorldState.Clear();
            foreach (var id in data.collectedEssenceIds)
                WorldState.CollectedIds.Add(id);
            foreach (var e in data.spawnerEntries)
                WorldState.SpawnerTimestamps[e.spawnerId] = e.collectedAtTicks;
        }

        /// <summary>Restores return point and realm seed before redirecting scenes.</summary>
        private static void PrepareSceneTransition(SaveData data)
        {
            if (!string.IsNullOrEmpty(data.returnScene))
                SceneTransitionData.SetReturn(data.returnScene,
                    new Vector3(data.returnPositionX, data.returnPositionY, data.returnPositionZ),
                    data.returnRotationY);

            if (!string.IsNullOrEmpty(data.realmBiome) &&
                System.Enum.TryParse<BiomeType>(data.realmBiome, out var biome))
                SceneTransitionData.SetRealm(biome, data.realmSeed);
        }

        private void RestoreCurrentScene()
        {
            RestoreMachinesForScene(_data);
            RestoreOreVeins(_data);

            if (_pendingPlayerRestore)
            {
                ApplyPlayerState(_data);
                _pendingPlayerRestore = false;
            }
        }

        private void ApplyPlayerState(SaveData data)
        {
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

            // Route the saved position through the destination mechanism so
            // SceneEntryPoint re-teleports to the SAME spot (instead of consuming
            // the pending return point, which the exit portal still needs).
            SceneTransitionData.SetDestination(pos, data.rotationY);

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
        //  Machine restore
        // ------------------------------------------------------------------ //

        private void RestoreMachinesForScene(SaveData data)
        {
            if (data.buildingEntries == null || data.buildingEntries.Count == 0) return;
            if (allMachines == null || allMachines.Count == 0)
            {
                Debug.LogWarning("[SaveManager] allMachines list is empty — cannot load machines. Assign MachineData assets in the inspector.");
                return;
            }

            string key = CurrentSceneKey();

            // Idempotency: never instantiate a machine whose GUID already exists
            // (protects against double restoration).
            var existingGuids = new HashSet<string>();
            foreach (var existing in FindObjectsByType<MachineGuid>(FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(existing.Guid))
                    existingGuids.Add(existing.Guid);

            var machinesByGuid = new Dictionary<string, IMachineConnectable>();
            var pipesByGuid = new Dictionary<string, SpiritPipe>();

            foreach (var entry in data.buildingEntries)
            {
                if (NormalizeKey(entry.sceneKey) != key) continue;

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

                if (existingGuids.Contains(guid)) continue;

                Vector3 position = new Vector3(entry.posX, entry.posY, entry.posZ);
                Quaternion rotation = Quaternion.Euler(0f, entry.rotation * 90f, 0f);

                GameObject placed = Instantiate(md.prefab, position, rotation);
                placed.name = md.machineName;

                // Restore persistent GUID
                var guidComp = placed.AddComponent<MachineGuid>();
                guidComp.SetGuid(guid);

                // Wire machine data (shared with PlacementController)
                MachineWiring.Wire(placed, md);

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

            RestoreMachineInventories(data, machinesByGuid);
            RestorePipeConnections(data, pipesByGuid, machinesByGuid);
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

            var veins = FindObjectsByType<OreVein>(FindObjectsSortMode.None);
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
