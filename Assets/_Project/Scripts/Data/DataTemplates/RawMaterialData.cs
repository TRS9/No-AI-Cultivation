using UnityEngine;

namespace CultivationGame.Data
{
    [CreateAssetMenu(fileName = "NewRawMaterial", menuName = "Cultivation/Raw Material Data")]
    public class RawMaterialData : ItemData
    {
        public string materialName;
        public Color materialColor = Color.white;
    }
}
