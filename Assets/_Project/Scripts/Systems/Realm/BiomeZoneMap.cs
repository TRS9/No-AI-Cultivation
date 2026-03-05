using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Stores Voronoi zone data for multi-biome realms.
    /// Maps each heightmap texel to a biome with blend weights for smooth transitions.
    /// </summary>
    public class BiomeZoneMap
    {
        private readonly int[,] _cellIndices;
        private readonly float[,,] _blendWeights;
        private readonly BiomeType[] _cellBiomes;
        private readonly int _resolution;
        private readonly int _cellCount;

        public BiomeZoneMap(int[,] cellIndices, float[,,] blendWeights, BiomeType[] cellBiomes,
                            int resolution)
        {
            _cellIndices  = cellIndices;
            _blendWeights = blendWeights;
            _cellBiomes   = cellBiomes;
            _resolution   = resolution;
            _cellCount    = cellBiomes.Length;
        }

        /// <summary>
        /// Returns the dominant BiomeType at a world position.
        /// worldPos is in world space; halfSize and terrainSize convert to normalized coords.
        /// </summary>
        public BiomeType GetBiomeAt(float worldX, float worldZ, float halfSize)
        {
            // World pos [-halfSize, halfSize] → normalized [0, 1]
            float nx = (worldX + halfSize) / (halfSize * 2f);
            float nz = (worldZ + halfSize) / (halfSize * 2f);

            int x = Mathf.Clamp(Mathf.RoundToInt(nx * (_resolution - 1)), 0, _resolution - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(nz * (_resolution - 1)), 0, _resolution - 1);

            int cellIndex = _cellIndices[z, x];
            return _cellBiomes[cellIndex];
        }

        /// <summary>
        /// Returns all unique BiomeTypes present in this zone map.
        /// </summary>
        public List<BiomeType> GetUniqueBiomes()
        {
            var unique = new List<BiomeType>();
            foreach (var b in _cellBiomes)
                if (!unique.Contains(b))
                    unique.Add(b);
            return unique;
        }

        /// <summary>
        /// Blends per-biome heightmaps using the stored Voronoi weights.
        /// The perBiomeHeights dictionary maps BiomeType → heightmap.
        /// Cells with the same BiomeType share the same heightmap.
        /// </summary>
        public float[,] BlendHeightmaps(Dictionary<BiomeType, float[,]> perBiomeHeights)
        {
            // Map each cell index to its heightmap
            var perCellHeights = new float[_cellCount][,];
            for (int c = 0; c < _cellCount; c++)
            {
                var biome = _cellBiomes[c];
                if (perBiomeHeights.TryGetValue(biome, out var h))
                    perCellHeights[c] = h;
                else
                    perCellHeights[c] = new float[_resolution, _resolution];
            }

            return HeightmapBuilder.BlendHeightmaps(perCellHeights, _blendWeights,
                                                     _resolution, _cellCount);
        }

        /// <summary>
        /// Fills outWeights with per-cell blend weights at a world position.
        /// Used for smooth splatmap transitions at biome boundaries.
        /// </summary>
        public void GetBlendWeightsAt(float worldX, float worldZ, float halfSize, float[] outWeights)
        {
            float nx = (worldX + halfSize) / (halfSize * 2f);
            float nz = (worldZ + halfSize) / (halfSize * 2f);

            // Bilinear interpolation for smooth splatmap sampling between texels
            float fx = Mathf.Clamp(nx * (_resolution - 1), 0f, _resolution - 1f);
            float fz = Mathf.Clamp(nz * (_resolution - 1), 0f, _resolution - 1f);

            int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, _resolution - 2);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, _resolution - 2);
            int x1 = x0 + 1;
            int z1 = z0 + 1;

            float tx = fx - x0;
            float tz = fz - z0;

            for (int c = 0; c < _cellCount && c < outWeights.Length; c++)
            {
                float w00 = _blendWeights[z0, x0, c];
                float w10 = _blendWeights[z0, x1, c];
                float w01 = _blendWeights[z1, x0, c];
                float w11 = _blendWeights[z1, x1, c];

                outWeights[c] = Mathf.Lerp(
                    Mathf.Lerp(w00, w10, tx),
                    Mathf.Lerp(w01, w11, tx),
                    tz);
            }
        }

        public int CellCount => _cellCount;
        public BiomeType[] CellBiomes => _cellBiomes;
    }
}
