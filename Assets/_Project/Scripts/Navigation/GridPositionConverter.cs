using RuntimeRoguelike.Configs;
using UnityEngine;
using RuntimeRoguelike.Ecs;

namespace RuntimeRoguelike
{
    public class GridPositionConverter
    {
        private readonly GridConfig _gridConfig;

        public float CellSize => Mathf.Max(0.01f, _gridConfig.CellSize);

        public GridPositionConverter(GridConfig gridConfig)
        {
            _gridConfig = gridConfig;
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPosition.x / CellSize), Mathf.FloorToInt(worldPosition.y / CellSize));
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3((cell.x + 0.5f) * CellSize, (cell.y + 0.5f) * CellSize, 0f);
        }

        public Vector3 CellToWorld(Float2 cell)
        {
            return new Vector3((cell.X + 0.5f) * CellSize, (cell.Y + 0.5f) * CellSize, 0f);
        }
    }
}
