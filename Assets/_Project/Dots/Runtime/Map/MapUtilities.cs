using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    public static class MapUtilities
    {
        public static int ToIndex(int2 cell, int2 size)
        {
            return cell.y * size.x + cell.x;
        }

        public static bool InBounds(int2 cell, int2 size)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < size.x && cell.y < size.y;
        }

        public static bool IsWalkable(ref  MapBlob map, int2 cell)
        {
            if (!InBounds(cell, map.Size))
            {
                return false;
            }

            var index = ToIndex(cell, map.Size);
            return map.BaseLayer[index] == MapCellType.Floor && map.ObstacleLayer[index] == ObstacleType.None;
        }
    }
}
