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

        // Building State
        public List<BuildingSaveEntry> buildingEntries = new List<BuildingSaveEntry>();

        // Pipe Connections
        public List<PipeConnectionSaveEntry> pipeConnections = new List<PipeConnectionSaveEntry>();

        // Machine Inventories
        public List<MachineInventorySaveEntry> machineInventories = new List<MachineInventorySaveEntry>();

        // Ore Vein State
        public List<OreVeinSaveEntry> oreVeinEntries = new List<OreVeinSaveEntry>();
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

    [Serializable]
    public class BuildingSaveEntry
    {
        public string machineId; // MachineData asset name
        public float posX, posY, posZ;
        public int rotation; // 0-3
    }

    [Serializable]
    public class PipeConnectionSaveEntry
    {
        public string pipeId;           // unique ID of the pipe building entry
        public string sourceBuildingId; // machineId of source building
        public float sourcePosX, sourcePosY, sourcePosZ;
        public string destBuildingId;   // machineId of destination building
        public float destPosX, destPosY, destPosZ;
        public string filterItemId;     // optional filter item
    }

    [Serializable]
    public class MachineInventorySaveEntry
    {
        public float machinePosX, machinePosY, machinePosZ; // position as identifier
        public string recipeId;  // current recipe name
        public List<InventorySaveEntry> inputItems = new List<InventorySaveEntry>();
        public List<InventorySaveEntry> outputItems = new List<InventorySaveEntry>();
    }

    [Serializable]
    public class OreVeinSaveEntry
    {
        public string veinId;
        public int remainingYield;
    }
}