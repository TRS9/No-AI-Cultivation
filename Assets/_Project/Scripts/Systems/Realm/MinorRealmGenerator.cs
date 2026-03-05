using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Procedurally generates the Minor Realm scene using a Unity Terrain.
    /// Supports biome-specific terrain shapes, logical water, caves, and multi-biome Voronoi zones.
    /// Runs in Awake (order -90) so it finishes before SceneEntryPoint.Start()
    /// positions the player.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class MinorRealmGenerator : MonoBehaviour
    {
        [Header("Biome Configs — assign one per biome")]
        [SerializeField] private MinorRealmConfig[] biomeConfigs;

        [Header("Shared Prefabs")]
        [SerializeField] private GameObject exitPortalPrefab;

        [Header("World Settings")]
        [SerializeField] private int   terrainSize         = 400;
        [SerializeField] private int   heightmapResolution = 257;
        [SerializeField] private Vector3 exitPortalOffset  = new Vector3(0f, 0f, 160f);

        [Header("Spawn Safety")]
        [SerializeField] private float spawnClearRadius = 12f;

        private Terrain _terrain;
        private float   _halfSize;
        private int     _terrainLayer;
        private int     _interactableLayer;
        private BiomeZoneMap _zoneMap;

        // -------------------------------------------------------------------------

        private void Awake()
        {
            var config = FindConfig(SceneTransitionData.RealmBiome);
            if (config == null)
            {
                Debug.LogWarning($"[MinorRealmGenerator] No config found for biome {SceneTransitionData.RealmBiome}");
                return;
            }

            _halfSize          = terrainSize / 2f;
            _terrainLayer      = LayerMask.NameToLayer("Terrain");
            _interactableLayer = LayerMask.NameToLayer("Interactable");

            Random.InitState(SceneTransitionData.RealmSeed);

            DetermineMultiBiome(config);
            _terrain = GenerateTerrain(config);
            GenerateWater(config);
            GenerateCaves(config);

            var poiCenters = GeneratePOIs(config);
            GenerateDecorations(config, poiCenters);

            PlaceExitPortal();
            ApplyAtmosphere(config);

            float centerHeight = _terrain.SampleHeight(Vector3.zero) + 1.5f;
            SceneTransitionData.SetDestination(new Vector3(0f, centerHeight, 0f));
        }

        // -------------------------------------------------------------------------
        // Multi-Biome
        // -------------------------------------------------------------------------

        private void DetermineMultiBiome(MinorRealmConfig config)
        {
            if (!config.multiBiome.allowMultiBiome) return;
            if (Random.value > config.multiBiome.multiBiomeChance) return;
            if (config.multiBiome.secondaryBiomes == null ||
                config.multiBiome.secondaryBiomes.Length == 0) return;

            int cellCount = Mathf.Max(3, config.multiBiome.voronoiCellCount);
            var centers = new Vector2[cellCount];

            centers[0] = new Vector2(0.5f + Random.Range(-0.1f, 0.1f),
                                     0.5f + Random.Range(-0.1f, 0.1f));

            for (int i = 1; i < cellCount; i++)
                centers[i] = new Vector2(Random.Range(0.05f, 0.95f), Random.Range(0.05f, 0.95f));

            var cellBiomes = new BiomeType[cellCount];
            cellBiomes[0] = config.biome;
            var secondary = config.multiBiome.secondaryBiomes;
            for (int i = 1; i < cellCount; i++)
                cellBiomes[i] = secondary[Random.Range(0, secondary.Length)];

            int blendRadius = Mathf.Max(5, config.multiBiome.blendRadius);
            HeightmapBuilder.BuildVoronoiMap(heightmapResolution, centers, blendRadius,
                                              out var indices, out var weights);

            _zoneMap = new BiomeZoneMap(indices, weights, cellBiomes, heightmapResolution);
        }

        // -------------------------------------------------------------------------
        // Terrain
        // -------------------------------------------------------------------------

        private Terrain GenerateTerrain(MinorRealmConfig config)
        {
            var terrainData = new TerrainData
            {
                heightmapResolution = heightmapResolution,
            };
            terrainData.size = new Vector3(terrainSize, config.maxHeight, terrainSize);

            float offX = Random.Range(0f, 1000f);
            float offZ = Random.Range(0f, 1000f);

            int   res = heightmapResolution;
            float[,] heights;

            if (_zoneMap != null)
            {
                var uniqueBiomes    = _zoneMap.GetUniqueBiomes();
                var perBiomeHeights = new Dictionary<BiomeType, float[,]>();

                foreach (var biome in uniqueBiomes)
                {
                    var biomeConfig = FindConfig(biome);
                    if (biomeConfig == null) biomeConfig = config;

                    perBiomeHeights[biome] = HeightmapBuilder.Build(
                        res, biomeConfig.shapeProfile, offX, offZ, biome);
                }

                heights = _zoneMap.BlendHeightmaps(perBiomeHeights);
            }
            else
            {
                heights = HeightmapBuilder.Build(res, config.shapeProfile, offX, offZ, config.biome);
            }

            // Post-processing
            if (config.shapeProfile.terraceSteps > 0)
                HeightmapBuilder.ApplyTerracing(heights,
                    config.shapeProfile.terraceSteps, config.shapeProfile.terraceSharpness);

            // Smooth after terracing to soften hard cliff edges while keeping shelf shapes
            HeightmapBuilder.SmoothHeightmap(heights, config.postTerraceSmoothIterations);

            if (config.water.enabled && config.water.beachBandWidth > 0f)
                HeightmapBuilder.ApplyBeachFlattening(heights, config.water.waterLevel,
                    config.water.beachBandWidth, config.water.beachFlattenStrength);

            // Raise terrain at map edges so water stays interior — use configurable width
            if (config.water.enabled)
            {
                float falloff = config.edgeFalloffWidth;
                float raiseTarget = config.water.waterLevel + 0.15f;
                HeightmapBuilder.ApplyEdgeFalloff(heights, res, falloff, raiseTarget);
            }

            // Carve lake basins AFTER edge falloff — Min() always wins over raised edges
            if (config.water.enabled && config.water.lakeCount > 0)
            {
                var lakeCenters = new Vector2[config.water.lakeCount];
                for (int i = 0; i < config.water.lakeCount; i++)
                    lakeCenters[i] = new Vector2(
                        Random.Range(0.25f, 0.75f),
                        Random.Range(0.25f, 0.75f));

                float radius = config.water.lakeRadius > 0.01f ? config.water.lakeRadius : 0.08f;
                float depth  = config.water.lakeDepth  > 0.01f ? config.water.lakeDepth  : 0.15f;
                HeightmapBuilder.CarveLakeBasins(heights, res, lakeCenters, radius,
                    config.water.waterLevel, depth);
            }

            // Final smoothing pass — softens all remaining sharp transitions
            float wl = config.water.enabled ? config.water.waterLevel : -1f;
            HeightmapBuilder.SmoothHeightmap(heights, config.smoothingIterations, wl);

            terrainData.SetHeights(0, 0, heights);

            // Compute slope map for texture blending
            var slopeMap = HeightmapBuilder.ComputeSlopeMap(heights, res, config.maxHeight, terrainSize);

            if (_zoneMap != null)
                ApplyMultiBiomeSplatmap(terrainData, config, slopeMap);
            else
                ApplySplatmap(terrainData, config, slopeMap);

            var go = Terrain.CreateTerrainGameObject(terrainData);
            go.name = "Terrain";
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(-_halfSize, 0f, -_halfSize);

            if (_terrainLayer != -1)
                go.layer = _terrainLayer;

            return go.GetComponent<Terrain>();
        }

        /// <summary>
        /// Single-biome splatmap: base terrain layer + cliff layer on steep slopes.
        /// </summary>
        private void ApplySplatmap(TerrainData terrainData, MinorRealmConfig config, float[,] slopeMap)
        {
            var layers = new List<TerrainLayer>();

            if (config.terrainLayers != null && config.terrainLayers.Length > 0)
                layers.Add(config.terrainLayers[0]);
            else
                return; // No layers to apply

            int cliffIdx = -1;
            if (config.cliffTerrainLayer != null)
            {
                cliffIdx = layers.Count;
                layers.Add(config.cliffTerrainLayer);
            }

            terrainData.terrainLayers = layers.ToArray();

            if (cliffIdx < 0) return; // No cliff layer — nothing to blend

            int alphamapRes = terrainData.alphamapResolution;
            int layerCount  = layers.Count;
            var alphamaps    = new float[alphamapRes, alphamapRes, layerCount];
            int hmRes        = slopeMap.GetLength(0);

            for (int z = 0; z < alphamapRes; z++)
            for (int x = 0; x < alphamapRes; x++)
            {
                // Map alphamap coords to heightmap coords
                int hz = Mathf.Clamp(z * (hmRes - 1) / (alphamapRes - 1), 0, hmRes - 1);
                int hx = Mathf.Clamp(x * (hmRes - 1) / (alphamapRes - 1), 0, hmRes - 1);
                float slope = slopeMap[hz, hx];

                float cliffBlend = ComputeCliffBlend(slope, config.slopeTextureThreshold, config.slopeBlendRange);
                alphamaps[z, x, 0]        = 1f - cliffBlend;
                alphamaps[z, x, cliffIdx] = cliffBlend;
            }

            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        /// <summary>
        /// Multi-biome splatmap: Voronoi biome blending + cliff layer on steep slopes.
        /// </summary>
        private void ApplyMultiBiomeSplatmap(TerrainData terrainData, MinorRealmConfig config, float[,] slopeMap)
        {
            var allLayers = new List<TerrainLayer>();
            var biomeLayerIndex = new Dictionary<BiomeType, int>();

            // Build layer list from all cell biomes
            var cellBiomes = _zoneMap.CellBiomes;
            for (int c = 0; c < cellBiomes.Length; c++)
            {
                var biome = cellBiomes[c];
                if (biomeLayerIndex.ContainsKey(biome)) continue;

                var bc = FindConfig(biome);
                if (bc == null || bc.terrainLayers == null || bc.terrainLayers.Length == 0) continue;

                var layer = bc.terrainLayers[0];
                if (!allLayers.Contains(layer))
                    allLayers.Add(layer);
                biomeLayerIndex[biome] = allLayers.IndexOf(layer);
            }

            if (allLayers.Count == 0) return;

            // Add cliff layer
            int cliffIdx = -1;
            if (config.cliffTerrainLayer != null)
            {
                cliffIdx = allLayers.Count;
                allLayers.Add(config.cliffTerrainLayer);
            }

            terrainData.terrainLayers = allLayers.ToArray();

            int alphamapRes = terrainData.alphamapResolution;
            int layerCount  = allLayers.Count;
            var alphamaps    = new float[alphamapRes, alphamapRes, layerCount];
            var cellWeights  = new float[_zoneMap.CellCount];
            int hmRes        = slopeMap.GetLength(0);

            for (int z = 0; z < alphamapRes; z++)
            for (int x = 0; x < alphamapRes; x++)
            {
                float nx = (float)x / (alphamapRes - 1);
                float nz = (float)z / (alphamapRes - 1);

                float worldX = (nx - 0.5f) * terrainSize;
                float worldZ = (nz - 0.5f) * terrainSize;

                // Biome blend weights
                _zoneMap.GetBlendWeightsAt(worldX, worldZ, _halfSize, cellWeights);

                for (int c = 0; c < _zoneMap.CellCount; c++)
                {
                    if (cellWeights[c] <= 0f) continue;
                    var biome = cellBiomes[c];
                    if (biomeLayerIndex.TryGetValue(biome, out int layerIdx))
                        alphamaps[z, x, layerIdx] += cellWeights[c];
                }

                // Blend cliff texture on steep slopes
                if (cliffIdx >= 0)
                {
                    int hz = Mathf.Clamp(z * (hmRes - 1) / (alphamapRes - 1), 0, hmRes - 1);
                    int hx = Mathf.Clamp(x * (hmRes - 1) / (alphamapRes - 1), 0, hmRes - 1);
                    float slope = slopeMap[hz, hx];
                    float cliffBlend = ComputeCliffBlend(slope, config.slopeTextureThreshold, config.slopeBlendRange);

                    if (cliffBlend > 0f)
                    {
                        // Scale down existing biome weights proportionally
                        for (int l = 0; l < layerCount; l++)
                        {
                            if (l != cliffIdx)
                                alphamaps[z, x, l] *= (1f - cliffBlend);
                        }
                        alphamaps[z, x, cliffIdx] = cliffBlend;
                    }
                }

                // Normalize weights to sum to 1
                float total = 0f;
                for (int l = 0; l < layerCount; l++)
                    total += alphamaps[z, x, l];

                if (total > 0.001f && Mathf.Abs(total - 1f) > 0.001f)
                    for (int l = 0; l < layerCount; l++)
                        alphamaps[z, x, l] /= total;
                else if (total < 0.001f)
                    alphamaps[z, x, 0] = 1f;
            }

            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        private static float ComputeCliffBlend(float slopeDegrees, float threshold, float blendRange)
        {
            if (slopeDegrees <= threshold) return 0f;
            float t = Mathf.Clamp01((slopeDegrees - threshold) / Mathf.Max(blendRange, 0.01f));
            return t * t * (3f - 2f * t); // smoothstep
        }

        // -------------------------------------------------------------------------
        // Water — single plane at water level
        // -------------------------------------------------------------------------

        private void GenerateWater(MinorRealmConfig config)
        {
            if (!config.water.enabled || config.water.waterPrefab == null) return;

            float waterY = config.water.waterLevel * config.maxHeight;
            var water = Instantiate(config.water.waterPrefab,
                new Vector3(0f, waterY, 0f), Quaternion.identity, transform);

            // Account for prefab's base mesh size (Unity Plane = 10x10, Quad = 1x1).
            // Measure actual renderer bounds to scale correctly.
            var renderer = water.GetComponent<Renderer>();
            if (renderer != null)
            {
                float meshSizeX = renderer.bounds.size.x / water.transform.localScale.x;
                float meshSizeZ = renderer.bounds.size.z / water.transform.localScale.z;
                if (meshSizeX > 0.01f && meshSizeZ > 0.01f)
                    water.transform.localScale = new Vector3(
                        terrainSize / meshSizeX,
                        water.transform.localScale.y,
                        terrainSize / meshSizeZ);
            }
            else
            {
                water.transform.localScale = new Vector3(terrainSize, 1f, terrainSize);
            }

            if (water.GetComponent<Collider>() == null)
            {
                var col = water.AddComponent<BoxCollider>();
                col.size   = new Vector3(1f, 0.1f, 1f);
                col.center = Vector3.zero;
            }

            if (_terrainLayer != -1)
                SetLayerRecursive(water, _terrainLayer, _interactableLayer);
        }

        // -------------------------------------------------------------------------
        // Caves — slope-aware placement
        // -------------------------------------------------------------------------

        private void GenerateCaves(MinorRealmConfig config)
        {
            if (config.caves.cavePrefabs == null || config.caves.cavePrefabs.Length == 0) return;
            if (config.caves.caveCount <= 0) return;

            float safeRadius     = _halfSize * 0.8f;
            float minCaveSpacing = 40f;
            var   placed         = new List<Vector2>();
            int   maxAttempts    = config.caves.caveCount * 40;
            int   attempts       = 0;
            int   spawned        = 0;

            while (spawned < config.caves.caveCount && attempts < maxAttempts)
            {
                attempts++;
                Vector2 candidate = Random.insideUnitCircle * safeRadius;

                if (candidate.magnitude < spawnClearRadius + 20f) continue;
                if (HasNeighbour(placed, candidate, minCaveSpacing)) continue;

                float groundY     = SampleY(candidate.x, candidate.y);
                float normalizedH = groundY / config.maxHeight;

                if (normalizedH < config.caves.minHeight || normalizedH > config.caves.maxHeight) continue;

                float slopeDeg = SampleSlope(candidate.x, candidate.y);
                if (slopeDeg < config.caves.minSlopeDegrees || slopeDeg > config.caves.maxSlopeDegrees) continue;

                var prefab = config.caves.cavePrefabs[spawned % config.caves.cavePrefabs.Length];
                if (prefab == null) continue;

                Vector3 normal     = SampleTerrainNormal(candidate.x, candidate.y);
                Vector3 flatNormal = new Vector3(normal.x, 0f, normal.z).normalized;
                Quaternion rot = flatNormal.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(flatNormal, Vector3.up)
                    : Quaternion.identity;

                var instance = TryInstantiate(prefab,
                    new Vector3(candidate.x, groundY, candidate.y), rot);

                if (instance != null)
                {
                    placed.Add(candidate);
                    spawned++;
                }
            }
        }

        private float SampleSlope(float worldX, float worldZ)
        {
            float d  = 1f;
            float hL = SampleY(worldX - d, worldZ);
            float hR = SampleY(worldX + d, worldZ);
            float hD = SampleY(worldX, worldZ - d);
            float hU = SampleY(worldX, worldZ + d);

            float dx = (hR - hL) / (2f * d);
            float dz = (hU - hD) / (2f * d);

            return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
        }

        private Vector3 SampleTerrainNormal(float worldX, float worldZ)
        {
            float d  = 1f;
            float hL = SampleY(worldX - d, worldZ);
            float hR = SampleY(worldX + d, worldZ);
            float hD = SampleY(worldX, worldZ - d);
            float hU = SampleY(worldX, worldZ + d);

            return new Vector3(hL - hR, 2f * d, hD - hU).normalized;
        }

        // -------------------------------------------------------------------------
        // Points of Interest — essences surrounded by decoration rings
        // -------------------------------------------------------------------------

        private List<Vector2> GeneratePOIs(MinorRealmConfig config)
        {
            var poiCenters = new List<Vector2>();

            if (config.essencePrefabs == null || config.essencePrefabs.Length == 0)
                return poiCenters;

            int   count       = Mathf.Min(config.poiCount, config.essenceCount);
            float safeRadius  = _halfSize * 0.8f;
            int   maxAttempts = count * 30;
            int   attempts    = 0;

            while (poiCenters.Count < count && attempts < maxAttempts)
            {
                attempts++;
                Vector2 candidate = Random.insideUnitCircle * safeRadius;

                if (candidate.magnitude < spawnClearRadius + 15f) continue;
                if (HasNeighbour(poiCenters, candidate, config.poiSpacing)) continue;

                poiCenters.Add(candidate);
            }

            for (int i = 0; i < poiCenters.Count; i++)
            {
                var   center  = poiCenters[i];
                float groundY = SampleY(center.x, center.y);
                var   pos     = new Vector3(center.x, groundY + 1f, center.y);
                var   prefab  = config.essencePrefabs[i % config.essencePrefabs.Length];

                if (prefab != null)
                    TryInstantiate(prefab, pos, Quaternion.identity, setTerrainLayer: false);

                PlacePOIRing(config, center, config.poiRingCount);
            }

            for (int i = count; i < config.essenceCount; i++)
            {
                Vector2 rng = Random.insideUnitCircle * safeRadius;
                if (rng.magnitude < spawnClearRadius) continue;

                float groundY = SampleY(rng.x, rng.y);
                var   pos     = new Vector3(rng.x, groundY + 1f, rng.y);
                var   prefab  = config.essencePrefabs[i % config.essencePrefabs.Length];
                if (prefab != null)
                    TryInstantiate(prefab, pos, Quaternion.identity, setTerrainLayer: false);
            }

            return poiCenters;
        }

        private void PlacePOIRing(MinorRealmConfig config, Vector2 center, int ringCount)
        {
            float ringRadius = 8f;
            float angleStep  = 360f / ringCount;

            for (int j = 0; j < ringCount; j++)
            {
                float angle = angleStep * j + Random.Range(-10f, 10f);
                float rad   = angle * Mathf.Deg2Rad;
                float r     = ringRadius + Random.Range(-2f, 2f);

                float x = center.x + Mathf.Cos(rad) * r;
                float z = center.y + Mathf.Sin(rad) * r;
                float groundY = SampleY(x, z);

                var prefab = PickDecorationForHeight(config, groundY / config.maxHeight);
                if (prefab == null) continue;

                var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                TryInstantiate(prefab, new Vector3(x, groundY, z), rot);
            }
        }

        // -------------------------------------------------------------------------
        // Decorations — clustered, height-aware, density-varied, zone-aware
        // -------------------------------------------------------------------------

        private void GenerateDecorations(MinorRealmConfig config, List<Vector2> poiCenters)
        {
            // Shared placement list — decorations from all biomes respect each other's spacing
            var placed = new List<Vector2>();

            if (_zoneMap != null)
            {
                // Multi-biome: each biome gets its own budget placed within its zone
                var uniqueBiomes = _zoneMap.GetUniqueBiomes();
                foreach (var biome in uniqueBiomes)
                {
                    var biomeConfig = FindConfig(biome);
                    if (biomeConfig == null) biomeConfig = config;
                    PlaceDecorationsForBiome(biomeConfig, biome, poiCenters, placed);
                }
            }
            else
            {
                // Single biome
                PlaceDecorationsForBiome(config, config.biome, poiCenters, placed);
            }
        }

        /// <summary>
        /// Places decorationCount decorations for a single biome.
        /// In multi-biome, only places within zones where this biome is dominant.
        /// </summary>
        private void PlaceDecorationsForBiome(MinorRealmConfig biomeConfig, BiomeType biome,
                                                List<Vector2> poiCenters, List<Vector2> placed)
        {
            bool hasDecorations =
                (biomeConfig.decorationPrefabs != null && biomeConfig.decorationPrefabs.Length > 0) ||
                (biomeConfig.decorationGroups != null && biomeConfig.decorationGroups.Length > 0);
            if (!hasDecorations) return;

            int   budget       = biomeConfig.decorationCount;
            float minDist      = biomeConfig.minDecorSpacing;
            float safeRadius   = _halfSize - 5f;
            float poiClearDist = 15f;
            int   clusterSize  = Mathf.Max(1, biomeConfig.clusterSize);
            float clusterSpread = biomeConfig.clusterSpread;
            int   maxAttempts  = budget * 10;
            int   attempts     = 0;
            int   spawned      = 0;

            while (spawned < budget && attempts < maxAttempts)
            {
                attempts++;

                Vector2 anchor = Random.insideUnitCircle * safeRadius;

                if (anchor.magnitude < spawnClearRadius) continue;
                if (IsInsidePOI(anchor, poiCenters, poiClearDist)) continue;

                // In multi-biome, only place within this biome's zone
                if (_zoneMap != null)
                {
                    var dominant = _zoneMap.GetBiomeAt(anchor.x, anchor.y, _halfSize);
                    if (dominant != biome) continue;
                }

                float normalizedHeight = SampleY(anchor.x, anchor.y) / biomeConfig.maxHeight;
                if (Random.value > HeightDensityCurve(normalizedHeight)) continue;

                int thisCluster = Random.Range(1, clusterSize + 1);

                for (int c = 0; c < thisCluster && spawned < budget; c++)
                {
                    Vector2 offset = (c == 0) ? Vector2.zero : Random.insideUnitCircle * clusterSpread;
                    Vector2 pos2D  = anchor + offset;

                    if (pos2D.magnitude > safeRadius) continue;
                    if (pos2D.magnitude < spawnClearRadius) continue;
                    if (HasNeighbour(placed, pos2D, minDist)) continue;

                    float groundY = SampleY(pos2D.x, pos2D.y);

                    // Skip steep slopes (terrace edges, cliff faces) to avoid floating decorations
                    if (SampleSlope(pos2D.x, pos2D.y) > 35f) continue;

                    float nHeight = groundY / biomeConfig.maxHeight;

                    var prefab = PickDecorationForHeight(biomeConfig, nHeight);
                    if (prefab == null) continue;

                    var rot      = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    var instance = TryInstantiate(prefab, new Vector3(pos2D.x, groundY, pos2D.y), rot);

                    if (instance != null)
                    {
                        placed.Add(pos2D);
                        spawned++;
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // Exit portal & atmosphere
        // -------------------------------------------------------------------------

        private void PlaceExitPortal()
        {
            if (exitPortalPrefab == null) return;

            float groundY = SampleY(exitPortalOffset.x, exitPortalOffset.z);
            var   pos      = new Vector3(exitPortalOffset.x, groundY, exitPortalOffset.z);
            var   portal   = Instantiate(exitPortalPrefab, pos, Quaternion.identity, transform);

            if (_interactableLayer != -1)
                SetLayerRecursive(portal, _interactableLayer, -1);

            var portalScript = portal.GetComponent<Portal>();
            if (portalScript == null)
                portalScript = portal.AddComponent<Portal>();
            portalScript.SetAsExitPortal();

            if (portal.GetComponent<Collider>() == null)
                portal.AddComponent<BoxCollider>();
        }

        private void ApplyAtmosphere(MinorRealmConfig config)
        {
            RenderSettings.fog          = true;
            RenderSettings.fogMode      = FogMode.ExponentialSquared;
            RenderSettings.fogColor     = config.fogColor;
            RenderSettings.fogDensity   = config.fogDensity;
            RenderSettings.ambientLight = config.ambientLight;
        }

        // -------------------------------------------------------------------------
        // Central instantiation — spawn safety + layer assignment
        // -------------------------------------------------------------------------

        private GameObject TryInstantiate(GameObject prefab, Vector3 position, Quaternion rotation,
                                          bool setTerrainLayer = true)
        {
            var instance = Instantiate(prefab, position, rotation, transform);

            if (OverlapsSpawn(instance))
            {
                Destroy(instance);
                return null;
            }

            if (setTerrainLayer && _terrainLayer != -1)
                SetLayerRecursive(instance, _terrainLayer, _interactableLayer);

            return instance;
        }

        private bool OverlapsSpawn(GameObject instance)
        {
            float centerY = _terrain.SampleHeight(Vector3.zero);
            var spawnBounds = new Bounds(
                new Vector3(0f, centerY + 1f, 0f),
                Vector3.one * spawnClearRadius * 2f
            );

            var renderers = instance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                if (r.bounds.Intersects(spawnBounds))
                    return true;

            var colliders = instance.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
                if (c.bounds.Intersects(spawnBounds))
                    return true;

            return false;
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private float SampleY(float worldX, float worldZ)
            => _terrain.SampleHeight(new Vector3(worldX, 0f, worldZ));

        private MinorRealmConfig FindConfig(BiomeType biome)
        {
            if (biomeConfigs == null) return null;
            foreach (var c in biomeConfigs)
                if (c != null && c.biome == biome) return c;
            return null;
        }

        private static bool HasNeighbour(List<Vector2> placed, Vector2 candidate, float minDist)
        {
            float sq = minDist * minDist;
            foreach (var p in placed)
                if ((p - candidate).sqrMagnitude < sq) return true;
            return false;
        }

        private static bool IsInsidePOI(Vector2 pos, List<Vector2> poiCenters, float clearRadius)
        {
            float sq = clearRadius * clearRadius;
            foreach (var poi in poiCenters)
                if ((pos - poi).sqrMagnitude < sq) return true;
            return false;
        }

        private static void SetLayerRecursive(GameObject root, int targetLayer, int preserveLayer)
        {
            if (root.layer != preserveLayer)
                root.layer = targetLayer;

            for (int i = 0; i < root.transform.childCount; i++)
                SetLayerRecursive(root.transform.GetChild(i).gameObject, targetLayer, preserveLayer);
        }

        private static GameObject PickDecorationForHeight(MinorRealmConfig config, float normalizedHeight)
        {
            bool hasGeneral = config.decorationPrefabs != null && config.decorationPrefabs.Length > 0;

            if (config.decorationGroups != null && config.decorationGroups.Length > 0)
            {
                int matchCount = 0;
                for (int i = 0; i < config.decorationGroups.Length; i++)
                {
                    var g = config.decorationGroups[i];
                    if (normalizedHeight >= g.minHeight && normalizedHeight <= g.maxHeight
                        && g.prefabs != null && g.prefabs.Length > 0)
                        matchCount++;
                }

                if (matchCount > 0)
                {
                    // When both groups and general prefabs exist, mix them (30% general)
                    if (hasGeneral && Random.value < 0.3f)
                        return config.decorationPrefabs[Random.Range(0, config.decorationPrefabs.Length)];

                    int pick = Random.Range(0, matchCount);
                    int seen = 0;
                    for (int i = 0; i < config.decorationGroups.Length; i++)
                    {
                        var g = config.decorationGroups[i];
                        if (normalizedHeight >= g.minHeight && normalizedHeight <= g.maxHeight
                            && g.prefabs != null && g.prefabs.Length > 0)
                        {
                            if (seen == pick)
                                return g.prefabs[Random.Range(0, g.prefabs.Length)];
                            seen++;
                        }
                    }
                }
            }

            if (hasGeneral)
                return config.decorationPrefabs[Random.Range(0, config.decorationPrefabs.Length)];

            return null;
        }

        private static float HeightDensityCurve(float h)
        {
            float curve = 4f * h * (1f - h);
            return Mathf.Clamp(curve, 0.15f, 1f);
        }
    }
}
