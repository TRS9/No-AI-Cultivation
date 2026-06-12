using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    [RequireComponent(typeof(MeshRenderer))]
    public class SpiritEssence : MonoBehaviour, IInteractable
    {
        public EssenceData essenceData;
        [SerializeField] [Tooltip("Unique identifier for this essence node, used to persist collection state across sessions.")] private string uniqueId;

        private MeshRenderer meshRenderer;
        private static MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(uniqueId) && WorldState.IsCollected(uniqueId))
            {
                Destroy(gameObject);
                return;
            }
            ApplyVisuals();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Never bake a GUID into the prefab asset itself — every instance
            // would silently share it, so collecting one essence would mark
            // all copies as collected.
            if (string.IsNullOrEmpty(uniqueId) &&
                !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
                uniqueId = System.Guid.NewGuid().ToString();
#endif
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
            if (essenceData == null) return;

            if (!string.IsNullOrEmpty(uniqueId))
                WorldState.MarkCollected(uniqueId);

            var inventory = user.GetComponent<IInventory>();
            inventory?.AddItem(essenceData);

            Destroy(gameObject);
        }
    }
}
