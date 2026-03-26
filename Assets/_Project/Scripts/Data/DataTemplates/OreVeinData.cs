using UnityEngine;

namespace CultivationGame.Data
{
    /// <summary>
    /// Configuration for an ore vein / resource deposit in the overworld.
    /// Defines what resource it yields, how much, and respawn behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "NewOreVein", menuName = "Cultivation/Ore Vein Data")]
    public class OreVeinData : ScriptableObject
    {
        [Header("Identity")]
        public string veinName;
        [TextArea] public string description;
        public Sprite icon;
        public Color veinColor = Color.gray;

        [Header("Resource")]
        [Tooltip("The raw material this vein produces")]
        public RawMaterialData resource;

        [Header("Yield")]
        [Tooltip("Total amount of resource in this vein before depletion")]
        public int totalYield = 100;

        [Tooltip("Amount extracted per extraction cycle")]
        public int yieldPerExtraction = 1;

        [Header("Respawn")]
        [Tooltip("If true, the vein regenerates over time after depletion")]
        public bool canRespawn = true;

        [Tooltip("Time in seconds to fully respawn after depletion")]
        public float respawnTimeSeconds = 600f;

        [Header("Requirements")]
        [Tooltip("Minimum cultivation realm to interact with this vein")]
        public RealmDefinition requiredRealm;
    }
}
