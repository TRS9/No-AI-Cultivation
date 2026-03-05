using UnityEngine;

namespace CultivationGame.Data
{
    [CreateAssetMenu(fileName = "NewEssence", menuName = "Cultivation/Essence Data")]
    public class EssenceData : ScriptableObject
    {
        public string essenceName;
        public string description;
        public int qiValue;
        public Color essenceColor = Color.white;
        public Sprite icon;

        public GameObject collectionEffect;
    }
}
