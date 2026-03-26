using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.Systems
{
    public class BuildGrid : MonoBehaviour
    {
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private LayerMask terrainLayer;

        private HashSet<Vector2Int> _occupiedCells = new();

        public float CellSize => cellSize;

        /// <summary>
        /// Snaps a world position to the nearest grid cell centre and
        /// raycasts downward to find the terrain height at that position.
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            int x = Mathf.RoundToInt(worldPosition.x / cellSize);
            int z = Mathf.RoundToInt(worldPosition.z / cellSize);
            float snappedX = x * cellSize;
            float snappedZ = z * cellSize;

            // Raycast down to find terrain height at snapped position
            float y = worldPosition.y;
            Ray ray = new Ray(new Vector3(snappedX, 1000f, snappedZ), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 2000f, terrainLayer))
                y = hit.point.y;

            return new Vector3(snappedX, y, snappedZ);
        }

        /// <summary>
        /// Converts a world position to a grid cell coordinate.
        /// </summary>
        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            int x = Mathf.RoundToInt(worldPosition.x / cellSize);
            int z = Mathf.RoundToInt(worldPosition.z / cellSize);
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// Returns true when every cell the machine would occupy is free.
        /// Rotation 0/2 keeps the original gridSize; 1/3 swaps width and depth.
        /// </summary>
        public bool CanPlace(Vector3 worldPosition, Vector2Int gridSize, int rotation)
        {
            Vector2Int origin = WorldToCell(worldPosition);
            Vector2Int size = (rotation % 2 == 0)
                ? gridSize
                : new Vector2Int(gridSize.y, gridSize.x);

            for (int gx = 0; gx < size.x; gx++)
            {
                for (int gz = 0; gz < size.y; gz++)
                {
                    Vector2Int cell = origin + new Vector2Int(gx, gz);
                    if (_occupiedCells.Contains(cell))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Marks the cells covered by a placed machine as occupied.
        /// </summary>
        public void OccupyCells(Vector3 worldPosition, Vector2Int gridSize, int rotation)
        {
            Vector2Int origin = WorldToCell(worldPosition);
            Vector2Int size = (rotation % 2 == 0)
                ? gridSize
                : new Vector2Int(gridSize.y, gridSize.x);

            for (int gx = 0; gx < size.x; gx++)
            {
                for (int gz = 0; gz < size.y; gz++)
                {
                    _occupiedCells.Add(origin + new Vector2Int(gx, gz));
                }
            }
        }

        /// <summary>
        /// Frees the cells that were occupied by a removed machine.
        /// </summary>
        public void FreeCells(Vector3 worldPosition, Vector2Int gridSize, int rotation)
        {
            Vector2Int origin = WorldToCell(worldPosition);
            Vector2Int size = (rotation % 2 == 0)
                ? gridSize
                : new Vector2Int(gridSize.y, gridSize.x);

            for (int gx = 0; gx < size.x; gx++)
            {
                for (int gz = 0; gz < size.y; gz++)
                {
                    _occupiedCells.Remove(origin + new Vector2Int(gx, gz));
                }
            }
        }

        /// <summary>Returns a copy of the occupied-cell set (for saving).</summary>
        public HashSet<Vector2Int> GetOccupiedCells() => new(_occupiedCells);

        /// <summary>Restores the occupied-cell set (for loading).</summary>
        public void LoadOccupiedCells(HashSet<Vector2Int> cells)
        {
            _occupiedCells = new HashSet<Vector2Int>(cells);
        }
    }
}
