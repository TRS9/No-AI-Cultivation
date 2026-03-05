using System;
using System.Collections.Generic;

namespace CultivationGame.Data
{
    [Serializable]
    public class SaveData
    {
        // Player Stats
        public double currentQi;
        public int currentRealmIndex;

        // Player Position
        public float positionX, positionY, positionZ;
        public float rotationY;

        // Inventar
        public List<InventorySaveEntry> inventoryEntries = new List<InventorySaveEntry>();

        // World State
        public List<string> collectedEssenceIds = new List<string>();
        public List<SpawnerSaveEntry> spawnerEntries = new List<SpawnerSaveEntry>();

        // Scene persistence — which scene was active, and where to return in the outer world
        public string currentScene;
        public string returnScene;
        public float returnPositionX, returnPositionY, returnPositionZ;
        public float returnRotationY;

        // Minor Realm persistence — recreate the exact same generated world on load
        public string realmBiome;   // BiomeType.ToString()
        public int    realmSeed;
    }

    [Serializable]
    public class InventorySaveEntry
    {
        public string essenceId; // Wir nutzen den Namen des ScriptableObjects als ID
        public int count;
    }

    [Serializable]
    public class SpawnerSaveEntry
    {
        public string spawnerId;
        public long collectedAtTicks;
    }
}