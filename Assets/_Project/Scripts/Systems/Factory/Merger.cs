using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Merger: pulls items from two source machines and combines them into one
    /// output inventory. Acts as a 2-input / 1-output routing node.
    /// Spirit Pipes can then pull from the Merger's OutputInventory to feed
    /// the next machine in the production chain.
    /// </summary>
    public class Merger : MonoBehaviour, IInteractable, IMachineConnectable
    {
        [Header("Machine Configuration")]
        [SerializeField] [Tooltip("ScriptableObject containing the machine's name, icon, and build cost.")] private MachineData machineData;

        [Header("Transfer Settings")]
        [SerializeField] [Tooltip("Seconds between item transfer ticks.")] private float transferInterval = 1f;
        [SerializeField] [Tooltip("Number of items pulled from each source per transfer tick.")] private int itemsPerTransfer = 1;

        [Header("Buffer Capacity")]
        [SerializeField] [Tooltip("Maximum number of items the output buffer can hold.")] private int bufferCapacity = 50;

        private MachineInventory _outputInventory;
        private float _transferTimer;
        private bool _nextIsA = true;

        private IMachineConnectable _sourceA;
        private IMachineConnectable _sourceB;

        // --- IMachineConnectable ---
        // Merger manages its own input pulling, so InputInventory is null.
        // OutputInventory is the merged buffer that downstream pipes consume.
        public MachineInventory InputInventory => null;
        public MachineInventory OutputInventory => _outputInventory;
        public MachineData MachineData => machineData;

        // --- Public API ---
        public IMachineConnectable SourceA => _sourceA;
        public IMachineConnectable SourceB => _sourceB;

        private void Awake()
        {
            _outputInventory = new MachineInventory(bufferCapacity);
        }

        private void Update()
        {
            if (_sourceA == null && _sourceB == null) return;

            _transferTimer += Time.deltaTime;
            if (_transferTimer >= transferInterval)
            {
                _transferTimer = 0f;
                MergeItems();
            }
        }

        /// <summary>
        /// Assign both input sources. Either may be null (that source is skipped).
        /// </summary>
        public void SetSources(IMachineConnectable sourceA, IMachineConnectable sourceB)
        {
            _sourceA = sourceA;
            _sourceB = sourceB;
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

        private void MergeItems()
        {
            for (int i = 0; i < itemsPerTransfer; i++)
            {
                if (!_outputInventory.HasSpace()) return;

                // Alternate between the two sources to give equal pull priority.
                bool pulled = false;
                if (_nextIsA)
                {
                    pulled = TryPullFrom(_sourceA);
                    if (!pulled) pulled = TryPullFrom(_sourceB);
                }
                else
                {
                    pulled = TryPullFrom(_sourceB);
                    if (!pulled) pulled = TryPullFrom(_sourceA);
                }

                _nextIsA = !_nextIsA;

                if (!pulled) break;
            }
        }

        private bool TryPullFrom(IMachineConnectable source)
        {
            if (source?.OutputInventory == null) return false;

            ItemData item = source.OutputInventory.GetFirstItem();
            if (item == null) return false;

            int removed = source.OutputInventory.TryRemove(item, 1);
            if (removed > 0)
            {
                _outputInventory.TryAdd(item, 1);
                return true;
            }
            return false;
        }
    }
}
