using UnityEngine;

namespace CultivationGame.Data
{
    [CreateAssetMenu(fileName = "NewEssence", menuName = "Cultivation/Essence Data")]
    public class EssenceData : ItemData
    {
        public string essenceName;
        public Color essenceColor = Color.white;
    }
}
