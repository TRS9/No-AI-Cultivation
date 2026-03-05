using System.Collections;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    [RequireComponent(typeof(MeshRenderer), typeof(Collider))]
    public class EssenceSpawner : MonoBehaviour, IInteractable
    {
        public EssenceData essenceData;
        public float respawnTimeSeconds = 300f;

        [SerializeField] private string uniqueId;

        private MeshRenderer meshRenderer;
        private Collider col;
        private bool isAvailable;
        private static MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            col = GetComponent<Collider>();
        }

        private void Start()
        {
            float remaining = WorldState.GetRemainingRespawn(uniqueId, respawnTimeSeconds);
            isAvailable = remaining <= 0f;

            if (isAvailable)
            {
                SetVisible(true);
            }
            else
            {
                SetVisible(false);
                StartCoroutine(RespawnCoroutine(remaining));
            }

            ApplyVisuals();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(uniqueId))
                uniqueId = System.Guid.NewGuid().ToString();

            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (essenceData == null || meshRenderer == null) return;

            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", essenceData.essenceColor);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        public void Interact(GameObject user)
        {
            if (!isAvailable || essenceData == null) return;

            var inventory = user.GetComponent<IInventory>();
            inventory?.AddItem(essenceData);

            WorldState.RecordSpawnerCollection(uniqueId);
            isAvailable = false;
            SetVisible(false);
            StartCoroutine(RespawnCoroutine(respawnTimeSeconds));
        }

        private IEnumerator RespawnCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            isAvailable = true;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (meshRenderer != null) meshRenderer.enabled = visible;
            if (col != null) col.enabled = visible;
        }
    }
}
