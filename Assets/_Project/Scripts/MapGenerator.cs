using System.Collections.Generic;
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

    public sealed class MapGenerator
    {
        private readonly int width;
        private readonly int height;
        private readonly int seed;
        private readonly int roomAttempts;
        private readonly int minRoomSize;
        private readonly int maxRoomSize;
        private readonly int safeRadius;

        public MapGenerator(int width, int height, int seed, int roomAttempts, int minRoomSize, int maxRoomSize, int safeRadius)
        {
            this.width = width;
            this.height = height;
            this.seed = seed;
            this.roomAttempts = roomAttempts;
            this.minRoomSize = minRoomSize;
            this.maxRoomSize = maxRoomSize;
            this.safeRadius = safeRadius;
        }

        public MapGenerationResult Generate()
        {
            var grid = new MapGrid(width, height);
            grid.FillBase(CellType.Wall);

            var rng = new System.Random(seed);
            var rooms = new List<RectInt>();

            for (var i = 0; i < roomAttempts; i++)
            {
                var roomWidth = rng.Next(minRoomSize, maxRoomSize + 1);
                var roomHeight = rng.Next(minRoomSize, maxRoomSize + 1);
                var roomX = rng.Next(1, width - roomWidth - 1);
                var roomY = rng.Next(1, height - roomHeight - 1);
                var room = new RectInt(roomX, roomY, roomWidth, roomHeight);

                if (IsOverlapping(room, rooms))
                {
                    continue;
                }

                CarveRoom(grid, room);

                if (rooms.Count > 0)
                {
                    var previousCenter = GetCenter(rooms[rooms.Count - 1]);
                    var currentCenter = GetCenter(room);
                    CarveCorridor(grid, previousCenter, currentCenter, rng);
                }

                rooms.Add(room);
            }

            if (rooms.Count == 0)
            {
                var fallbackRoom = new RectInt(width / 2 - 4, height / 2 - 4, 8, 8);
                CarveRoom(grid, fallbackRoom);
                rooms.Add(fallbackRoom);
            }

            var startCell = GetCenter(rooms[0]);
            EnsureSafeRadius(grid, startCell, safeRadius);
            grid.SetBorderWalls();

            return new MapGenerationResult(grid, startCell, safeRadius);
        }

        private static void CarveRoom(MapGrid grid, RectInt room)
        {
            for (var y = room.yMin; y < room.yMax; y++)
            {
                for (var x = room.xMin; x < room.xMax; x++)
                {
                    grid.Set(x, y, CellType.Floor);
                }
            }
        }

        private static void CarveCorridor(MapGrid grid, Vector2Int from, Vector2Int to, System.Random rng)
        {
            if (rng.NextDouble() < 0.5)
            {
                CarveHorizontal(grid, from.x, to.x, from.y);
                CarveVertical(grid, from.y, to.y, to.x);
            }
            else
            {
                CarveVertical(grid, from.y, to.y, from.x);
                CarveHorizontal(grid, from.x, to.x, to.y);
            }
        }

        private static void CarveHorizontal(MapGrid grid, int x0, int x1, int y)
        {
            var start = Mathf.Min(x0, x1);
            var end = Mathf.Max(x0, x1);
            for (var x = start; x <= end; x++)
            {
                grid.Set(x, y, CellType.Floor);
            }
        }

        private static void CarveVertical(MapGrid grid, int y0, int y1, int x)
        {
            var start = Mathf.Min(y0, y1);
            var end = Mathf.Max(y0, y1);
            for (var y = start; y <= end; y++)
            {
                grid.Set(x, y, CellType.Floor);
            }
        }

        private static Vector2Int GetCenter(RectInt rect)
        {
            return new Vector2Int(rect.x + rect.width / 2, rect.y + rect.height / 2);
        }

        private static bool IsOverlapping(RectInt room, List<RectInt> rooms)
        {
            var expanded = new RectInt(room.xMin - 1, room.yMin - 1, room.width + 2, room.height + 2);
            for (var i = 0; i < rooms.Count; i++)
            {
                if (expanded.Overlaps(rooms[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureSafeRadius(MapGrid grid, Vector2Int center, int radius)
        {
            for (var y = center.y - radius; y <= center.y + radius; y++)
            {
                for (var x = center.x - radius; x <= center.x + radius; x++)
                {
                    if (!grid.InBounds(x, y))
                    {
                        continue;
                    }

                    var dx = x - center.x;
                    var dy = y - center.y;
                    if (dx * dx + dy * dy > radius * radius)
                    {
                        continue;
                    }

                    grid.Set(x, y, CellType.Floor);
                    grid.SetObstacle(x, y, ObstacleType.None);
                    grid.SetHazard(x, y, HazardType.None);
                }
            }
        }
    }
}
