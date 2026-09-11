using UnityEngine;
using CultivationGame.Data;
using CultivationGame.Core;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Shared helper that assigns a MachineData asset to whichever machine
    /// component a placed prefab carries. Used by PlacementController (placement)
    /// and SaveManager (loading) so the component list only exists once.
    /// </summary>
    public static class MachineWiring
    {
        /// <summary>Ensures visual-only prefabs receive the same behavior on placement and load.</summary>
        public static bool Wire(GameObject placed, MachineData data)
        {
            if (placed == null || data == null) return false;
            EnsureMachineComponent(placed, data.machineType);

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
            else
                return false;

            return true;
        }

        private static void EnsureMachineComponent(GameObject placed, MachineType type)
        {
            switch (type)
            {
                case MachineType.Furnace:
                case MachineType.Crusher:
                case MachineType.Mixer:
                case MachineType.Distiller:
                case MachineType.Condenser:
                case MachineType.PillPress:
                    if (!placed.GetComponent<BaseMachine>()) placed.AddComponent<BaseMachine>();
                    break;
                case MachineType.ResourceExtractor:
                    if (!placed.GetComponent<ResourceExtractor>()) placed.AddComponent<ResourceExtractor>();
                    break;
                case MachineType.Storage:
                    if (!placed.GetComponent<StorageContainer>()) placed.AddComponent<StorageContainer>();
                    break;
                case MachineType.QiConduit:
                    if (!placed.GetComponent<QiConduit>()) placed.AddComponent<QiConduit>();
                    break;
                case MachineType.Splitter:
                    if (!placed.GetComponent<Splitter>()) placed.AddComponent<Splitter>();
                    break;
                case MachineType.Merger:
                    if (!placed.GetComponent<Merger>()) placed.AddComponent<Merger>();
                    break;
                case MachineType.SpiritPipe:
                    if (!placed.GetComponent<SpiritPipe>()) placed.AddComponent<SpiritPipe>();
                    break;
            }
        }
    }
}
