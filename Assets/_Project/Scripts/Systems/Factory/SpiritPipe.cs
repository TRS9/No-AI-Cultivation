using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Spirit Pipe: connects the output of one machine to the input of another.
    /// Periodically transfers items from source output to destination input.
    /// Placed on the grid like any other machine.
    /// </summary>
    public class SpiritPipe : MonoBehaviour, IInteractable
    {
        [Header("Connection")]
        [SerializeField] private BaseMachine sourceMachine;
        [SerializeField] private BaseMachine destinationMachine;

        [Header("Transport Settings")]
        [SerializeField] private float transferInterval = 1f;
        [SerializeField] private int itemsPerTransfer = 1;

        [Header("Filter (optional)")]
        [Tooltip("If set, only this item type is transported. Leave null for any item.")]
        [SerializeField] private ItemData filterItem;

        private float _transferTimer;
        private bool _isConnected;

        // --- Public API ---
        public BaseMachine SourceMachine => sourceMachine;
        public BaseMachine DestinationMachine => destinationMachine;
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
        public void Connect(BaseMachine source, BaseMachine destination)
        {
            sourceMachine = source;
            destinationMachine = destination;
            _isConnected = source != null && destination != null;

            if (_isConnected)
                GameDataEvents.RaisePipeConnected(this, source, destination);
        }

        public void Disconnect()
        {
            var oldSource = sourceMachine;
            var oldDest = destinationMachine;
            sourceMachine = null;
            destinationMachine = null;
            _isConnected = false;

            if (oldSource != null || oldDest != null)
                GameDataEvents.RaisePipeDisconnected(this);
        }

        public void SetFilter(ItemData item)
        {
            filterItem = item;
        }

        public void Interact(GameObject user)
        {
            // Open pipe configuration UI (connect source/destination)
            GameDataEvents.RaisePipeInteracted(this);
        }

        private void TransferItems()
        {
            if (sourceMachine == null || destinationMachine == null) return;

            var sourceOutput = sourceMachine.OutputInventory;
            var destInput = destinationMachine.InputInventory;

            if (sourceOutput == null || destInput == null) return;

            for (int i = 0; i < itemsPerTransfer; i++)
            {
                ItemData itemToTransfer;

                if (filterItem != null)
                {
                    // Transfer only the filtered item
                    if (!sourceOutput.HasItem(filterItem)) return;
                    itemToTransfer = filterItem;
                }
                else
                {
                    // Transfer any available item
                    itemToTransfer = sourceOutput.GetFirstItem();
                    if (itemToTransfer == null) return;
                }

                if (!destInput.HasSpace()) return;

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
