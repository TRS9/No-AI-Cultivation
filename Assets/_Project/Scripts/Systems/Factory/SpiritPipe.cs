using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Spirit Pipe: connects the output of one machine to the input of another.
    /// Periodically transfers items from source output to destination input.
    /// Works with any IMachineConnectable (BaseMachine, StorageContainer, ResourceExtractor).
    /// </summary>
    public class SpiritPipe : MonoBehaviour, IInteractable
    {
        [Header("Machine Identity")]
        [SerializeField] private MachineData machineData;

        [Header("Transport Settings")]
        [SerializeField] private float transferInterval = 1f;
        [SerializeField] private int itemsPerTransfer = 1;

        [Header("Filter (optional)")]
        [Tooltip("If set, only this item type is transported. Leave null for any item.")]
        [SerializeField] private ItemData filterItem;

        private IMachineConnectable _source;
        private IMachineConnectable _destination;
        private float _transferTimer;
        private bool _isConnected;

        // --- Public API ---
        public MachineData MachineData => machineData;
        public IMachineConnectable Source => _source;
        public IMachineConnectable Destination => _destination;
        public bool IsConnected => _isConnected;
        public ItemData FilterItem => filterItem;

        private void Update()
        {
            if (!_isConnected) return;

            _transferTimer += Time.deltaTime;
            if (_transferTimer >= transferInterval)
            {
                _transferTimer = 0f;
                TransferItems();
            }
        }

        /// <summary>
        /// Connect this pipe between two machines.
        /// Called during placement or when loading from save.
        /// </summary>
        public void Connect(IMachineConnectable source, IMachineConnectable destination)
        {
            _source = source;
            _destination = destination;
            _isConnected = source != null && destination != null
                        && source.OutputInventory != null
                        && destination.InputInventory != null;

            if (_isConnected)
                GameDataEvents.RaisePipeConnected(this, source as MonoBehaviour, destination as MonoBehaviour);
        }

        public void Disconnect()
        {
            var oldSource = _source as MonoBehaviour;
            _source = null;
            _destination = null;
            _isConnected = false;

            if (oldSource != null)
                GameDataEvents.RaisePipeDisconnected(this);
        }

        public void SetFilter(ItemData item)
        {
            filterItem = item;
        }

        public void SetMachineData(MachineData data)
        {
            machineData = data;
        }

        public void Interact(GameObject user)
        {
            GameDataEvents.RaisePipeInteracted(this);
        }

        private void TransferItems()
        {
            if (_source == null || _destination == null) return;

            // Self-heal: if either endpoint machine was destroyed (removed by the
            // player), its C# inventory object would otherwise keep accepting items
            // into the void. Disconnect instead of transferring into a ghost buffer.
            if ((_source as MonoBehaviour) == null || (_destination as MonoBehaviour) == null)
            {
                Disconnect();
                return;
            }

            var sourceOutput = _source.OutputInventory;
            var destInput = _destination.InputInventory;

            if (sourceOutput == null || destInput == null) return;

            // Respect destination filter (e.g. StorageContainer.AcceptsItem)
            StorageContainer destStorage = _destination as StorageContainer;

            for (int i = 0; i < itemsPerTransfer; i++)
            {
                ItemData itemToTransfer;

                if (filterItem != null)
                {
                    if (!sourceOutput.HasItem(filterItem)) return;
                    itemToTransfer = filterItem;
                }
                else
                {
                    itemToTransfer = sourceOutput.GetFirstItem();
                    if (itemToTransfer == null) return;
                }

                if (!destInput.HasSpace()) return;

                // Check destination storage filter before transferring
                if (destStorage != null && !destStorage.AcceptsItem(itemToTransfer)) return;

                int removed = sourceOutput.TryRemove(itemToTransfer, 1);
                if (removed > 0)
                {
                    destInput.TryAdd(itemToTransfer, 1);
                }
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }
}
