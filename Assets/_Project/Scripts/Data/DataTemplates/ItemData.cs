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
        public string description;
        public int qiValue;
        public Sprite icon;
        public GameObject collectionEffect;
        public ItemType itemType;
    }
}
