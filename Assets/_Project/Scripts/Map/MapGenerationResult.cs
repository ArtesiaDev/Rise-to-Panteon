using UnityEngine;

namespace RuntimeRoguelike
{
    public readonly struct MapGenerationResult
    {
        public MapGrid Grid { get; }
        public Vector2Int StartCell { get; }
        public int SafeRadius { get; }

        public MapGenerationResult(MapGrid grid, Vector2Int startCell, int safeRadius)
        {
            Grid = grid;
            StartCell = startCell;
            SafeRadius = safeRadius;
        }
    }
}
