using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Common interface for anything a Spirit Pipe can connect to:
    /// BaseMachine, StorageContainer, ResourceExtractor, etc.
    /// InputInventory may be null for output-only machines (e.g. ResourceExtractor).
    /// OutputInventory may be null for input-only machines (future use).
    /// </summary>
    public interface IMachineConnectable
    {
        MachineInventory InputInventory { get; }
        MachineInventory OutputInventory { get; }
        MachineData MachineData { get; }
    }
}
