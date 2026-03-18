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
        }
    }
}
