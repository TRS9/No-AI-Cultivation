using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data
{
    [System.Serializable]
    public struct NoiseOctave
    {
        [Range(0.5f, 30f)] public float frequency;
        [Range(0f, 1f)]    public float amplitude;
    }

    [System.Serializable]
    public struct TerrainShapeProfile
    {
        [Tooltip("Noise octaves that build the base heightmap. If empty, uses default 3-octave Perlin.")]
        public NoiseOctave[] octaves;

        [Tooltip("0 = pure Perlin, 1 = full ridge noise (abs-inverted Perlin). Creates sharp mountain ridges.")]
        [Range(0f, 1f)] public float ridgeWeight;

        [Tooltip("Terrace step count. 0 = no terracing. Higher = more cliff steps (mesas, plateaus).")]
        [Range(0, 20)] public int terraceSteps;

        [Tooltip("How sharp terrace edges are. 0 = smooth ramps, 1 = hard cliff steps.")]
        [Range(0f, 1f)] public float terraceSharpness;

        [Tooltip("Amplifies height variation around midpoint. >1 = deeper valleys + taller peaks. <1 = flatter, more uniform. 0 = treated as 1.")]
        [Range(0.2f, 4f)] public float heightContrast;

        [Tooltip("Normalized height floor. Everything below is clamped up.")]
        [Range(0f, 0.5f)] public float heightFloor;

        [Tooltip("Normalized height ceiling. 0 = treated as 1 (no ceiling).")]
        [Range(0f, 1f)] public float heightCeiling;
    }

    [System.Serializable]
    public struct WaterConfig
    {
        [Tooltip("If true, places a single water plane at waterLevel covering the entire realm.")]
        public bool enabled;

        public GameObject waterPrefab;

        [Tooltip("Normalized water level (0-1 of maxHeight). Terrain below this is underwater.")]
        [Range(0f, 0.5f)] public float waterLevel;

        [Tooltip("Width of the beach flattening band above waterLevel (normalized height).")]
        [Range(0f, 0.15f)] public float beachBandWidth;

        [Tooltip("How much terrain in the beach band flattens toward waterLevel. 0=none, 1=fully flat.")]
        [Range(0f, 1f)] public float beachFlattenStrength;

        [Tooltip("Number of lake basins carved into terrain. Lakes form independently of heightContrast.")]
        [Range(0, 8)] public int lakeCount;

        [Tooltip("Radius of each lake basin as a fraction of map size. 0.05 = small pond, 0.15 = large lake.")]
        [Range(0.03f, 0.2f)] public float lakeRadius;

        [Tooltip("How deep lake basins are carved below waterLevel. Higher = deeper lakes.")]
        [Range(0f, 0.4f)] public float lakeDepth;
    }

    [System.Serializable]
    public struct CaveConfig
    {
        public GameObject[] cavePrefabs;
        [Range(0, 6)] public int caveCount;

        [Tooltip("Minimum terrain slope (degrees) for cave placement.")]
        [Range(10f, 60f)] public float minSlopeDegrees;

        [Tooltip("Maximum terrain slope (degrees) for cave placement.")]
        [Range(20f, 80f)] public float maxSlopeDegrees;

        [Tooltip("Minimum normalized height for cave placement.")]
        [Range(0f, 1f)] public float minHeight;

        [Tooltip("Maximum normalized height for cave placement.")]
        [Range(0f, 1f)] public float maxHeight;
    }

    [System.Serializable]
    public struct MultiBiomeConfig
    {
        [Tooltip("If true, this realm can become multi-biome (decided by seed-based random chance).")]
        public bool allowMultiBiome;

        [Tooltip("Chance (0-1) that this realm becomes multi-biome.")]
        [Range(0f, 1f)] public float multiBiomeChance;

        [Tooltip("Secondary biomes that can appear as Voronoi zones alongside the primary biome.")]
        public BiomeType[] secondaryBiomes;

        [Tooltip("Number of Voronoi cells. More = smaller zones. Typically 4-8.")]
        [Range(3, 12)] public int voronoiCellCount;

        [Tooltip("Width of the blending band at zone boundaries, in heightmap texels.")]
        [Range(5, 40)] public int blendRadius;
    }

    [System.Serializable]
    public struct DecorationGroup
    {
        public string label;
        public GameObject[] prefabs;
        [Range(0f, 1f)] public float minHeight;
        [Range(0f, 1f)] public float maxHeight;
    }

    [CreateAssetMenu(fileName = "NewRealmConfig", menuName = "Cultivation/Minor Realm Config")]
    public class MinorRealmConfig : ScriptableObject
    {
        [Header("Identity")]
        public BiomeType biome;

        [Header("Terrain Shape")]
        public TerrainShapeProfile shapeProfile;

        [Header("Terrain")]
        public TerrainLayer[] terrainLayers;
        public float maxHeight = 20f;

        [Header("Water")]
        public WaterConfig water;

        [Header("Caves")]
        public CaveConfig caves;

        [Header("Multi-Biome")]
        public MultiBiomeConfig multiBiome;

        [Header("Decorations")]
        public GameObject[] decorationPrefabs;
        public DecorationGroup[] decorationGroups;
        public int decorationCount = 60;
        [Min(1f)] public float minDecorSpacing = 2f;

        [Header("Points of Interest")]
        [Range(3, 8)] public int poiCount = 4;
        [Range(30f, 80f)] public float poiSpacing = 50f;
        [Range(3, 8)] public int poiRingCount = 6;

        [Header("Clustering")]
        [Range(2, 5)] public int clusterSize = 3;
        [Range(1f, 5f)] public float clusterSpread = 3f;

        [Header("Essences")]
        public GameObject[] essencePrefabs;
        public int essenceCount = 5;

        [Header("Heightmap Smoothing")]
        [Tooltip("Box-blur iterations on the final heightmap. Higher = smoother terrain.")]
        [Range(0, 10)] public int smoothingIterations = 2;

        [Tooltip("Extra smoothing iterations applied right after terracing to soften cliff edges.")]
        [Range(0, 5)] public int postTerraceSmoothIterations = 1;

        [Header("Slope Texturing")]
        [Tooltip("Slope angle (degrees) where cliff/rock texture begins blending in.")]
        [Range(15f, 60f)] public float slopeTextureThreshold = 30f;

        [Tooltip("Degrees over which the blend transitions from base texture to cliff texture.")]
        [Range(1f, 20f)] public float slopeBlendRange = 10f;

        [Tooltip("TerrainLayer used on steep slopes. Assign TL_Rock or similar.")]
        public TerrainLayer cliffTerrainLayer;

        [Header("Edge Falloff")]
        [Tooltip("Width of the edge falloff border as fraction of terrain (0-0.5). Larger = gentler boundaries.")]
        [Range(0.05f, 0.5f)] public float edgeFalloffWidth = 0.25f;

        [Header("Atmosphere")]
        public Color fogColor = new Color(0.7f, 0.75f, 0.8f);
        [Range(0f, 0.05f)] public float fogDensity = 0.01f;
        public Color ambientLight = Color.white;
    }
}
