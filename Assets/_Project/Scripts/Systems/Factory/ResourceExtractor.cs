using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// A machine placed on or near an OreVein that automatically extracts resources.
    /// Extracted items go into the output inventory, which can be connected via Spirit Pipes.
    /// </summary>
    public class ResourceExtractor : MonoBehaviour, IInteractable, IMachineConnectable
    {
        [Header("Machine Configuration")]
        [SerializeField] [Tooltip("ScriptableObject containing the machine's name, icon, and build cost.")] private MachineData machineData;

        [Header("Extraction Settings")]
        [SerializeField] [Tooltip("Seconds between extraction attempts.")] private float extractionInterval = 3f;
        [SerializeField] [Tooltip("Number of items extracted per interval.")] private int amountPerExtraction = 1;
        [SerializeField] [Tooltip("World-space radius used to detect nearby OreVeins.")] private float detectionRadius = 4f;

        [Header("Output")]
        [SerializeField] [Tooltip("Maximum number of items the output inventory can hold.")] private int outputCapacity = 50;

        private MachineInventory _outputInventory;
        private OreVein _targetVein;
        private float _extractionTimer;

        // --- Public API ---
        public MachineData MachineData => machineData;
        public MachineInventory InputInventory => null; // output-only machine
        public MachineInventory OutputInventory => _outputInventory;
        public OreVein TargetVein => _targetVein;
        public bool HasTarget => _targetVein != null && !_targetVein.IsDepleted;

        private void Awake()
        {
            _outputInventory = new MachineInventory(outputCapacity);
        }

        private void Start()
        {
            FindNearestVein();
        }

        private void Update()
        {
            if (_targetVein == null || _targetVein.IsDepleted)
            {
                // Try to find a new vein periodically
                _extractionTimer += Time.deltaTime;
                if (_extractionTimer >= extractionInterval)
                {
                    _extractionTimer = 0f;
                    FindNearestVein();
                }
                return;
            }

            _extractionTimer += Time.deltaTime;
            if (_extractionTimer >= extractionInterval)
            {
                _extractionTimer = 0f;
                TryExtract();
            }
        }

        public void SetMachineData(MachineData data)
        {
            machineData = data;
            if (data != null && data.processingSpeed > 0f)
            {
                extractionInterval = Mathf.Max(0.5f, extractionInterval / data.processingSpeed);
            }
        }

        public void Interact(GameObject user)
        {
            GameDataEvents.RaiseMachineInteracted(this);
        }

        private void FindNearestVein()
        {
            var colliders = Physics.OverlapSphere(transform.position, detectionRadius);
            float closestDist = float.MaxValue;
            OreVein closest = null;

            foreach (var col in colliders)
            {
                var vein = col.GetComponent<OreVein>();
                if (vein == null || vein.IsDepleted) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = vein;
                }
            }

            _targetVein = closest;
        }

        private void TryExtract()
        {
            if (_targetVein == null || _targetVein.VeinData == null) return;
            if (!_outputInventory.HasSpace(amountPerExtraction)) return;

            int extracted = _targetVein.Extract(amountPerExtraction);
            if (extracted > 0 && _targetVein.VeinData.resource != null)
            {
                _outputInventory.TryAdd(_targetVein.VeinData.resource, extracted);
                GameDataEvents.RaiseResourceExtracted(_targetVein.VeinData.resource, extracted);
            }
        }
    }
}
