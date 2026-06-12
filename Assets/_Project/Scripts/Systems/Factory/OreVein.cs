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
        [SerializeField] [Tooltip("Unique identifier for this vein, used to persist depletion state across sessions.")] private string uniqueId;

        private MeshRenderer _meshRenderer;
        private Collider _collider;
        private int _remainingYield;
        private bool _isDepleted;
        private bool _loadedFromSave;
        private static MaterialPropertyBlock _propertyBlock;

        // --- Public API ---
        public OreVeinData VeinData => veinData;
        public int RemainingYield => _remainingYield;
        public bool IsDepleted => _isDepleted;
        public string UniqueId => uniqueId;

        /// <summary>
        /// Restores the persisted yield. Called by SaveManager before Start();
        /// Start() must not overwrite this state again.
        /// </summary>
        public void LoadRemainingYield(int yield)
        {
            _remainingYield = yield;
            _isDepleted = yield <= 0;
            _loadedFromSave = true;
            ApplyVisuals();
        }

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<Collider>();
        }

        /// <summary>
        /// Called after procedural spawning to assign vein configuration at runtime.
        /// Pass a deterministic <paramref name="persistentId"/> (e.g. derived from the
        /// realm seed) so the same vein keeps the same identity across regenerations.
        /// </summary>
        public void Initialize(OreVeinData data, string persistentId = null)
        {
            veinData = data;
            if (!string.IsNullOrEmpty(persistentId))
                uniqueId = persistentId;
            else if (string.IsNullOrEmpty(uniqueId))
                uniqueId = System.Guid.NewGuid().ToString();
        }

        private void Start()
        {
            if (veinData == null) return;

            if (_loadedFromSave)
            {
                // Saved state already applied — only resume a pending respawn timer.
                if (_isDepleted && veinData.canRespawn)
                {
                    float remainingTime = WorldState.GetRemainingRespawn(uniqueId, veinData.respawnTimeSeconds);
                    if (remainingTime > 0f)
                        StartCoroutine(RespawnCoroutine(remainingTime));
                    else
                        Respawn(); // timer elapsed while the game was closed
                }
                ApplyVisuals();
                return;
            }

            // Check if this vein was depleted and is still respawning
            float remaining = WorldState.GetRemainingRespawn(uniqueId, veinData.respawnTimeSeconds);
            if (remaining > 0f)
            {
                _isDepleted = true;
                _remainingYield = 0;
                StartCoroutine(RespawnCoroutine(remaining));
            }
            else
            {
                _remainingYield = veinData.totalYield;
                _isDepleted = false;
            }

            ApplyVisuals();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Never bake a GUID into the prefab asset itself — every instance
            // would silently share it and corrupt collection/respawn persistence.
            if (string.IsNullOrEmpty(uniqueId) &&
                !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
                uniqueId = System.Guid.NewGuid().ToString();
#endif
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
            if (amount <= 0) return;

            inventory.AddItem(veinData.resource, amount);
            _remainingYield -= amount;

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
            ApplyVisuals();

            if (veinData.canRespawn)
            {
                WorldState.RecordSpawnerCollection(uniqueId);
                StartCoroutine(RespawnCoroutine(veinData.respawnTimeSeconds));
            }
        }

        private IEnumerator RespawnCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            Respawn();
        }

        private void Respawn()
        {
            _remainingYield = veinData.totalYield;
            _isDepleted = false;
            ApplyVisuals();
        }

        /// <summary>
        /// Single source of truth for the vein color — depleted veins are dimmed.
        /// (Previously SetDepleted and ApplyVisuals fought over the same property.)
        /// </summary>
        private void ApplyVisuals()
        {
            if (_meshRenderer == null) return;

            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            Color color = veinData != null ? veinData.veinColor : Color.gray;
            if (_isDepleted) color *= 0.3f;

            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
