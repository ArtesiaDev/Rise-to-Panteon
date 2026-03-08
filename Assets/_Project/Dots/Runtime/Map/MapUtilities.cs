using Unity.Entities;
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

        public static bool IsWalkable(ref MapBlob map, int2 cell)
        {
            if (!InBounds(cell, map.Size))
            {
                return false;
            }

            var index = ToIndex(cell, map.Size);
            return map.BaseLayer[index] == MapCellType.Floor && map.ObstacleLayer[index] == ObstacleType.None;
        }

        /// <summary>
        /// Проверяет, является ли клетка стеной.
        /// </summary>
        public static bool IsWall(ref MapBlob map, int2 cell)
        {
            if (!InBounds(cell, map.Size))
            {
                return true; // За пределами карты считаем стеной
            }

            var index = ToIndex(cell, map.Size);
            return map.BaseLayer[index] == MapCellType.Wall;
        }

        /// <summary>
        /// Возвращает индекс комнаты, содержащей клетку, или -1 если клетка не в комнате.
        /// </summary>
        public static int GetRoomAt(ref MapBlob map, int2 cell)
        {
            for (var i = 0; i < map.RoomCount; i++)
            {
                ref var room = ref map.Rooms[i];
                var bounds = room.Bounds; // x, y, width, height
                if (cell.x >= bounds.x && cell.x < bounds.x + bounds.z &&
                    cell.y >= bounds.y && cell.y < bounds.y + bounds.w)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
