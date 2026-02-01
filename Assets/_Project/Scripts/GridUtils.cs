using UnityEngine;

namespace RuntimeRoguelike
{
    public static class GridUtils
    {
        public static Vector2Int WorldToCell(Vector3 worldPosition)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));
        }

        public static Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }
    }
}
