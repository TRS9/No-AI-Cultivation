using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Splitter: takes items from one source and distributes them round-robin
    /// between two destination machines. Acts as a 1-input / 2-output routing node.
    /// Spirit Pipes can push items into the Splitter's InputInventory; the Splitter
    /// then forwards items alternately to destinationA and destinationB.
    /// </summary>
    public class Splitter : MonoBehaviour, IInteractable, IMachineConnectable
    {
        [Header("Machine Configuration")]
        [SerializeField] private MachineData machineData;

        [Header("Transfer Settings")]
        [SerializeField] private float transferInterval = 1f;
        [SerializeField] private int itemsPerTransfer = 1;

        [Header("Buffer Capacity")]
        [SerializeField] private int bufferCapacity = 50;

        private MachineInventory _inputInventory;
        private float _transferTimer;
        private bool _nextIsA = true;

        private IMachineConnectable _destinationA;
        private IMachineConnectable _destinationB;

        // --- IMachineConnectable ---
        // InputInventory is the internal buffer that upstream pipes push into.
        // Splitter manages its own output routing, so OutputInventory is null.
        public MachineInventory InputInventory => _inputInventory;
        public MachineInventory OutputInventory => null;
        public MachineData MachineData => machineData;

        // --- Public API ---
        public IMachineConnectable DestinationA => _destinationA;
        public IMachineConnectable DestinationB => _destinationB;

        private void Awake()
        {
            _inputInventory = new MachineInventory(bufferCapacity);
        }

        private void Update()
        {
            if (_destinationA == null && _destinationB == null) return;

            _transferTimer += Time.deltaTime;
            if (_transferTimer >= transferInterval)
            {
                _transferTimer = 0f;
                DistributeItems();
            }
        }

        /// <summary>
        /// Assign both output destinations. Either may be null (output is skipped if null).
        /// </summary>
        public void SetDestinations(IMachineConnectable destA, IMachineConnectable destB)
        {
            _destinationA = destA;
            _destinationB = destB;
        }

        /// <summary>
        /// Set the machine configuration data (called by PlacementController on placement).
        /// </summary>
        public void SetMachineData(MachineData data)
        {
            machineData = data;
        }

        public void Interact(GameObject user)
        {
            GameDataEvents.RaiseMachineInteracted(this);
        }

        // --- Internal Logic ---

        private void DistributeItems()
        {
            for (int i = 0; i < itemsPerTransfer; i++)
            {
                ItemData item = _inputInventory.GetFirstItem();
                if (item == null) return;

                // Try sending to the preferred destination, fall back to the other.
                if (_nextIsA)
                {
                    if (!TryForward(item, _destinationA))
                        TryForward(item, _destinationB);
                }
                else
                {
                    if (!TryForward(item, _destinationB))
                        TryForward(item, _destinationA);
                }

                _nextIsA = !_nextIsA;
            }
        }

        private bool TryForward(ItemData item, IMachineConnectable destination)
        {
            if (destination?.InputInventory == null) return false;
            if (!destination.InputInventory.HasSpace()) return false;

            int removed = _inputInventory.TryRemove(item, 1);
            if (removed > 0)
            {
                destination.InputInventory.TryAdd(item, 1);
                return true;
            }
            return false;
        }
    }
}
