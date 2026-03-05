using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    public static class HeightmapBuilder
    {
        // Default octaves used when none are configured — gentler frequencies for natural terrain
        private static readonly NoiseOctave[] DefaultOctaves =
        {
            new NoiseOctave { frequency = 1.8f,  amplitude = 0.60f },
            new NoiseOctave { frequency = 4.0f,  amplitude = 0.25f },
            new NoiseOctave { frequency = 8.5f,  amplitude = 0.15f },
        };

        /// <summary>
        /// Builds a heightmap from a TerrainShapeProfile. All values normalized [0,1].
        /// Handles zero/default struct values gracefully.
        /// </summary>
        public static float[,] Build(int resolution, TerrainShapeProfile profile,
                                      float seedOffX, float seedOffZ, BiomeType biome)
        {
            // Sanitize struct defaults (all zero when not configured)
            float contrast      = profile.heightContrast > 0.01f ? profile.heightContrast : 1f;
            float heightFloor   = profile.heightFloor;
            float heightCeiling = profile.heightCeiling > 0.01f ? profile.heightCeiling : 1f;
            if (heightCeiling <= heightFloor) { heightFloor = 0f; heightCeiling = 1f; }

            var octaves = (profile.octaves != null && profile.octaves.Length > 0)
                ? profile.octaves : DefaultOctaves;

            var heights = new float[resolution, resolution];

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float nx = (float)x / (resolution - 1);
                float nz = (float)z / (resolution - 1);

                float h = SampleNoise(nx, nz, octaves, profile.ridgeWeight, seedOffX, seedOffZ);

                // Sigmoid contrast — smooth S-curve that amplifies peaks/valleys
                // without clipping to flat plateaus like linear contrast does.
                if (contrast != 1f)
                {
                    float centered = h * 2f - 1f;
                    float g = centered * contrast;
                    float s = g / (1f + Mathf.Abs(g));
                    h = s * 0.5f + 0.5f;
                }

                // Clamp to floor/ceiling
                h = Mathf.Clamp(h, heightFloor, heightCeiling);

                // Remap clamped range back to [0,1]
                float range = heightCeiling - heightFloor;
                if (range > 0.001f)
                    h = (h - heightFloor) / range;

                heights[z, x] = h;
            }

            // Biome-specific modifiers
            ApplyBiomeModifiers(heights, resolution, biome, seedOffX);

            return heights;
        }

        private static float SampleNoise(float nx, float nz, NoiseOctave[] octaves,
                                          float ridgeWeight, float offX, float offZ)
        {
            float h = 0f;
            float totalAmplitude = 0f;

            for (int i = 0; i < octaves.Length; i++)
            {
                var octave = octaves[i];
                float freq = octave.frequency;
                float amp  = octave.amplitude;
                if (freq <= 0f || amp <= 0f) continue;

                float ox = offX + i * 100f;
                float oz = offZ + i * 100f;

                float perlin = Mathf.PerlinNoise(nx * freq + ox, nz * freq + oz);
                float ridged = 1f - Mathf.Abs(perlin * 2f - 1f);

                float sample = Mathf.Lerp(perlin, ridged, ridgeWeight);
                h += sample * amp;
                totalAmplitude += amp;
            }

            if (totalAmplitude > 0f)
                h /= totalAmplitude;

            return Mathf.Clamp01(h);
        }

        private static void ApplyBiomeModifiers(float[,] heights, int resolution, BiomeType biome,
                                                 float seedOff)
        {
            switch (biome)
            {
                case BiomeType.NebulaBeach:
                    ApplyCoastalGradient(heights, resolution, seedOff);
                    break;
            }
        }

        private static void ApplyCoastalGradient(float[,] heights, int resolution, float seedOff)
        {
            float angle = (seedOff % 360f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float nx = (float)x / (resolution - 1) - 0.5f;
                float nz = (float)z / (resolution - 1) - 0.5f;

                float dot = nx * dir.x + nz * dir.y;
                float gradient = Mathf.Clamp01(dot + 0.5f);
                gradient = gradient * gradient * (3f - 2f * gradient);

                heights[z, x] *= gradient;
            }
        }

        // -------------------------------------------------------------------------
        // Post-processing passes
        // -------------------------------------------------------------------------

        public static void ApplyTerracing(float[,] heights, int steps, float sharpness)
        {
            if (steps <= 0) return;

            int resZ = heights.GetLength(0);
            int resX = heights.GetLength(1);

            for (int z = 0; z < resZ; z++)
            for (int x = 0; x < resX; x++)
            {
                float h = heights[z, x];
                float stepped = Mathf.Floor(h * steps) / steps;
                heights[z, x] = Mathf.Lerp(h, stepped, sharpness);
            }
        }

        public static void ApplyBeachFlattening(float[,] heights, float waterLevel,
                                                 float bandWidth, float strength)
        {
            if (bandWidth <= 0f || strength <= 0f) return;

            int resZ = heights.GetLength(0);
            int resX = heights.GetLength(1);

            float bandTop = waterLevel + bandWidth;

            for (int z = 0; z < resZ; z++)
            for (int x = 0; x < resX; x++)
            {
                float h = heights[z, x];
                if (h <= waterLevel || h >= bandTop) continue;

                float t = (h - waterLevel) / bandWidth;
                t = t * t * (3f - 2f * t);

                float flattened = Mathf.Lerp(waterLevel, h, t);
                heights[z, x] = Mathf.Lerp(h, flattened, strength);
            }
        }

        /// <summary>
        /// Carves bowl-shaped depressions into terrain to form lake basins.
        /// Lakes go below waterLevel regardless of heightContrast settings.
        /// </summary>
        public static void CarveLakeBasins(float[,] heights, int resolution,
                                             Vector2[] lakeCenters, float lakeRadius,
                                             float waterLevel, float lakeDepth)
        {
            if (lakeCenters == null || lakeCenters.Length == 0) return;
            if (lakeRadius <= 0f || lakeDepth <= 0f) return;

            float radiusSq = lakeRadius * lakeRadius;

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float nx = (float)x / (resolution - 1);
                float nz = (float)z / (resolution - 1);

                for (int i = 0; i < lakeCenters.Length; i++)
                {
                    float dx = nx - lakeCenters[i].x;
                    float dz = nz - lakeCenters[i].y;
                    float distSq = dx * dx + dz * dz;

                    if (distSq >= radiusSq) continue;

                    float t = Mathf.Sqrt(distSq) / lakeRadius; // 0 at center, 1 at edge
                    t = t * t * (3f - 2f * t); // smoothstep — smooth bowl shape

                    // Carve down: center of lake is deepest, edges blend back to original
                    float target = waterLevel - lakeDepth * (1f - t);
                    heights[z, x] = Mathf.Min(heights[z, x], target);
                }
            }
        }

        /// <summary>
        /// Raises terrain near map edges so water stays as interior lakes.
        /// falloffWidth: fraction of map that ramps (0.15 = outer 15%).
        /// raiseTarget: normalized height that edges get pushed toward (should be above waterLevel).
        /// </summary>
        public static void ApplyEdgeFalloff(float[,] heights, int resolution,
                                              float falloffWidth, float raiseTarget)
        {
            if (falloffWidth <= 0f) return;

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float nx = (float)x / (resolution - 1);
                float nz = (float)z / (resolution - 1);

                // Distance to nearest edge [0 at edge, 0.5 at center]
                float edgeDist = Mathf.Min(nx, 1f - nx, nz, 1f - nz);

                if (edgeDist >= falloffWidth) continue;

                float t = edgeDist / falloffWidth;
                t = t * t * (3f - 2f * t); // smoothstep

                float h = heights[z, x];
                heights[z, x] = Mathf.Lerp(Mathf.Max(h, raiseTarget), h, t);
            }
        }

        // -------------------------------------------------------------------------
        // Voronoi multi-biome
        // -------------------------------------------------------------------------

        public static void BuildVoronoiMap(int resolution, Vector2[] cellCenters, int blendRadius,
                                            out int[,] cellIndices, out float[,,] blendWeights)
        {
            int cellCount = cellCenters.Length;
            cellIndices  = new int[resolution, resolution];
            blendWeights = new float[resolution, resolution, cellCount];

            // Blend radius in normalized [0,1] space — NOT squared
            float blendRadiusNorm = blendRadius / (float)(resolution - 1);

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float nx = (float)x / (resolution - 1);
                float nz = (float)z / (resolution - 1);

                float minDist  = float.MaxValue;
                float minDist2 = float.MaxValue;
                int   nearest  = 0;
                int   second   = 0;

                for (int c = 0; c < cellCount; c++)
                {
                    float dx = nx - cellCenters[c].x;
                    float dz = nz - cellCenters[c].y;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz); // actual Euclidean distance

                    if (dist < minDist)
                    {
                        minDist2 = minDist; second  = nearest;
                        minDist  = dist;    nearest = c;
                    }
                    else if (dist < minDist2)
                    {
                        minDist2 = dist; second = c;
                    }
                }

                cellIndices[z, x] = nearest;

                // True distance to the Voronoi boundary between the two nearest cells.
                // The boundary is where d1 == d2, so boundary distance = (d2 - d1) / 2.
                float boundaryDist = (minDist2 - minDist) * 0.5f;

                if (cellCount == 1 || boundaryDist >= blendRadiusNorm)
                {
                    // Deep inside a cell — pure nearest biome, no blending
                    blendWeights[z, x, nearest] = 1f;
                }
                else
                {
                    // Near a boundary — smooth blend between ONLY the two nearest cells.
                    // t=0 at boundary, t=1 at full interior; smoothstep gives S-curve gradient.
                    float t = boundaryDist / blendRadiusNorm;
                    t = t * t * (3f - 2f * t); // smoothstep
                    blendWeights[z, x, nearest] = t;
                    blendWeights[z, x, second]  = 1f - t;
                }
            }
        }

        public static float[,] BlendHeightmaps(float[][,] perCellHeights, float[,,] blendWeights,
                                                 int resolution, int cellCount)
        {
            var result = new float[resolution, resolution];

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float h = 0f;
                for (int c = 0; c < cellCount; c++)
                    h += perCellHeights[c][z, x] * blendWeights[z, x, c];
                result[z, x] = h;
            }

            return result;
        }

        // -------------------------------------------------------------------------
        // Smoothing
        // -------------------------------------------------------------------------

        /// <summary>
        /// Applies a 3x3 box-blur to the heightmap. Multiple iterations = stronger smoothing.
        /// When waterLevel >= 0, cells at/below water are not raised above it (preserves lakes).
        /// </summary>
        public static void SmoothHeightmap(float[,] heights, int iterations, float waterLevel = -1f)
        {
            if (iterations <= 0) return;

            int resZ = heights.GetLength(0);
            int resX = heights.GetLength(1);
            var buffer = new float[resZ, resX];

            for (int iter = 0; iter < iterations; iter++)
            {
                System.Array.Copy(heights, buffer, heights.Length);

                for (int z = 1; z < resZ - 1; z++)
                for (int x = 1; x < resX - 1; x++)
                {
                    float sum = buffer[z - 1, x - 1] + buffer[z - 1, x] + buffer[z - 1, x + 1]
                              + buffer[z,     x - 1] + buffer[z,     x] + buffer[z,     x + 1]
                              + buffer[z + 1, x - 1] + buffer[z + 1, x] + buffer[z + 1, x + 1];
                    float smoothed = sum / 9f;

                    // Preserve lakes: don't raise underwater cells above water
                    if (waterLevel >= 0f && buffer[z, x] <= waterLevel)
                        smoothed = Mathf.Min(smoothed, waterLevel);

                    heights[z, x] = smoothed;
                }
            }
        }

        /// <summary>
        /// Applies extra smoothing only to cells near the water level.
        /// Cells far from water (outside lo..hi band) are left untouched,
        /// keeping hilltops and distant cliffs sharp.
        /// </summary>
        public static void SmoothNearWaterLevel(float[,] heights, float waterLevel,
                                                  float bandWidth, int iterations)
        {
            if (iterations <= 0 || bandWidth <= 0f) return;

            int resZ = heights.GetLength(0);
            int resX = heights.GetLength(1);
            var buffer = new float[resZ, resX];

            float lo = waterLevel - bandWidth;
            float hi = waterLevel + bandWidth;

            for (int iter = 0; iter < iterations; iter++)
            {
                System.Array.Copy(heights, buffer, heights.Length);

                for (int z = 1; z < resZ - 1; z++)
                for (int x = 1; x < resX - 1; x++)
                {
                    float h = buffer[z, x];
                    if (h < lo || h > hi) continue; // far from water — leave untouched

                    float sum = buffer[z - 1, x - 1] + buffer[z - 1, x] + buffer[z - 1, x + 1]
                              + buffer[z,     x - 1] + buffer[z,     x] + buffer[z,     x + 1]
                              + buffer[z + 1, x - 1] + buffer[z + 1, x] + buffer[z + 1, x + 1];
                    float smoothed = sum / 9f;

                    // Preserve lake beds — don't raise underwater cells
                    if (h <= waterLevel)
                        smoothed = Mathf.Min(smoothed, waterLevel);

                    heights[z, x] = smoothed;
                }
            }
        }

        /// <summary>
        /// Computes terrain slope in degrees at each heightmap cell.
        /// </summary>
        public static float[,] ComputeSlopeMap(float[,] heights, int resolution,
                                                 float maxHeight, float terrainSize)
        {
            var slopes = new float[resolution, resolution];
            float cellSize = terrainSize / (resolution - 1);

            for (int z = 1; z < resolution - 1; z++)
            for (int x = 1; x < resolution - 1; x++)
            {
                float dhdx = (heights[z, x + 1] - heights[z, x - 1]) * maxHeight / (2f * cellSize);
                float dhdz = (heights[z + 1, x] - heights[z - 1, x]) * maxHeight / (2f * cellSize);
                slopes[z, x] = Mathf.Atan(Mathf.Sqrt(dhdx * dhdx + dhdz * dhdz)) * Mathf.Rad2Deg;
            }

            // Copy edges from neighbors
            for (int x = 0; x < resolution; x++)
            {
                slopes[0, x] = slopes[1, x];
                slopes[resolution - 1, x] = slopes[resolution - 2, x];
            }
            for (int z = 0; z < resolution; z++)
            {
                slopes[z, 0] = slopes[z, 1];
                slopes[z, resolution - 1] = slopes[z, resolution - 2];
            }

            return slopes;
        }
    }
}
