using System.Collections;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// A resource deposit in the overworld. Can be manually interacted with
    /// (gives a small amount) or connected to a ResourceExtractor for automated mining.
    /// Follows the EssenceSpawner pattern for respawn and persistence.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer), typeof(Collider))]
    public class OreVein : MonoBehaviour, IInteractable
    {
        [Header("Configuration")]
        public OreVeinData veinData;

        [Header("Persistence")]
        [SerializeField] private string uniqueId;

        private MeshRenderer _meshRenderer;
        private Collider _collider;
        private int _remainingYield;
        private bool _isDepleted;
        private static MaterialPropertyBlock _propertyBlock;

        // --- Public API ---
        public OreVeinData VeinData => veinData;
        public int RemainingYield => _remainingYield;
        public bool IsDepleted => _isDepleted;
        public string UniqueId => uniqueId;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<Collider>();
        }

        /// <summary>
        /// Called after procedural spawning to assign vein configuration at runtime.
        /// </summary>
        public void Initialize(OreVeinData data)
        {
            veinData = data;
            if (string.IsNullOrEmpty(uniqueId))
                uniqueId = System.Guid.NewGuid().ToString();
        }

        private void Start()
        {
            if (veinData == null) return;

            // Check if this vein was depleted and is still respawning
            float remaining = WorldState.GetRemainingRespawn(uniqueId, veinData.respawnTimeSeconds);
            if (remaining > 0f)
            {
                _isDepleted = true;
                _remainingYield = 0;
                SetDepleted(true);
                StartCoroutine(RespawnCoroutine(remaining));
            }
            else
            {
                _remainingYield = veinData.totalYield;
                _isDepleted = false;
                SetDepleted(false);
            }

            ApplyVisuals();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(uniqueId))
                uniqueId = System.Guid.NewGuid().ToString();

            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            ApplyVisuals();
        }

        /// <summary>
        /// Manual interaction: player collects a small amount by hand.
        /// </summary>
        public void Interact(GameObject user)
        {
            if (_isDepleted || veinData == null || veinData.resource == null) return;

            var inventory = user.GetComponent<IInventory>();
            if (inventory == null) return;

            int amount = Mathf.Min(veinData.yieldPerExtraction, _remainingYield);
            for (int i = 0; i < amount; i++)
                inventory.AddItem(veinData.resource);

            _remainingYield -= amount;

            if (amount > 0)
                GameDataEvents.RaiseResourceExtracted(veinData.resource, amount);

            if (_remainingYield <= 0)
                Deplete();
        }

        /// <summary>
        /// Called by ResourceExtractor to extract resources automatically.
        /// Returns the actual amount extracted.
        /// </summary>
        public int Extract(int requestedAmount)
        {
            if (_isDepleted || veinData == null || veinData.resource == null) return 0;

            int amount = Mathf.Min(requestedAmount, _remainingYield);
            _remainingYield -= amount;

            if (_remainingYield <= 0)
                Deplete();

            return amount;
        }

        private void Deplete()
        {
            _isDepleted = true;
            _remainingYield = 0;
            SetDepleted(true);

            if (veinData.canRespawn)
            {
                WorldState.RecordSpawnerCollection(uniqueId);
                StartCoroutine(RespawnCoroutine(veinData.respawnTimeSeconds));
            }
        }

        private IEnumerator RespawnCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _remainingYield = veinData.totalYield;
            _isDepleted = false;
            SetDepleted(false);
        }

        private void SetDepleted(bool depleted)
        {
            // Visually indicate depletion (dim the vein)
            if (_meshRenderer != null)
            {
                if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
                _meshRenderer.GetPropertyBlock(_propertyBlock);
                Color color = veinData != null ? veinData.veinColor : Color.gray;
                _propertyBlock.SetColor("_BaseColor", depleted ? color * 0.3f : color);
                _meshRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void ApplyVisuals()
        {
            if (veinData == null || _meshRenderer == null) return;
            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", veinData.veinColor);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
