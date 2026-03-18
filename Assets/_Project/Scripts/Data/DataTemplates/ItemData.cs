using UnityEngine;

namespace CultivationGame.Data
{
    public enum ItemType
    {
        Essence,
        Pill,
        RawMaterial
    }

    public abstract class ItemData : ScriptableObject
    {
        [SerializeField, Tooltip("Stable save ID — set once, never rename. Falls back to asset name if empty.")]
        private string itemId;

        /// <summary>Stable identifier used for save/load. Falls back to the asset's file name.</summary>
        public string ItemId => string.IsNullOrEmpty(itemId) ? name : itemId;

        public string description;
        public int qiValue;
        public Sprite icon;
        public GameObject collectionEffect;
        public ItemType itemType;
    }
}
