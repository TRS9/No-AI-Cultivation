using UnityEngine;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Shared helper that assigns a MachineData asset to whichever machine
    /// component a placed prefab carries. Used by PlacementController (placement)
    /// and SaveManager (loading) so the component list only exists once.
    /// </summary>
    public static class MachineWiring
    {
        /// <summary>Returns false when no recognized machine component was found.</summary>
        public static bool Wire(GameObject placed, MachineData data)
        {
            if (placed == null) return false;

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
    }
}
