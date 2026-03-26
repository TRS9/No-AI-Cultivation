using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data
{
    [CreateAssetMenu(fileName = "NewMachine", menuName = "Cultivation/Machine Data")]
    public class MachineData : ScriptableObject
    {
        [Header("Identity")]
        public string machineName;
        [TextArea] public string description;
        public Sprite icon;
        public MachineType machineType;

        [Header("Prefabs")]
        public GameObject prefab;
        public GameObject ghostPrefab;

        [Header("Grid")]
        public Vector2Int gridSize = new Vector2Int(1, 1);

        [Header("Build Cost")]
        public RecipeIngredient[] buildCost;

        [Header("Machine Stats")]
        public float processingSpeed = 1f;
        public int inputSlots = 1;
        public int outputSlots = 1;
    }
}
