using UnityEngine;

namespace CultivationGame.Data
{
    public enum PillGrade { Mortal, Earth, Heaven, Divine }

    [CreateAssetMenu(fileName = "NewPill", menuName = "Cultivation/Pill Data")]
    public class PillData : ItemData
    {
        public string pillName;
        public Color pillColor = Color.white;
        public PillGrade grade;
    }
}
