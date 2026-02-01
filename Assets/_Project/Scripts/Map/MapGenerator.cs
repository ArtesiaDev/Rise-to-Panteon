using System.Collections.Generic;
using RuntimeRoguelike.Configs;
using UnityEngine;

namespace RuntimeRoguelike
{
    public class MapGenerator
    {
        private readonly MapGenerationConfig _config;

        public MapGenerator(MapGenerationConfig config)
        {
            _config = config;
        }

        public MapGenerationResult Generate(Vector2Int mapSize, int seed)
        {
            var width = Mathf.Max(10, mapSize.x);
            var height = Mathf.Max(10, mapSize.y);
            var grid = new MapGrid(width, height);
            grid.FillBase(CellType.Wall);

            var rng = new System.Random(seed);
            var rooms = new List<RectInt>();

            for (var i = 0; i < _config.RoomAttempts; i++)
            {
                var roomWidth = rng.Next(_config.MinRoomSize, _config.MaxRoomSize + 1);
                var roomHeight = rng.Next(_config.MinRoomSize, _config.MaxRoomSize + 1);
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
                var fallbackSize = _config.FallbackRoomSize;
                var fallbackRoom = new RectInt(width / 2 - fallbackSize.x / 2, height / 2 - fallbackSize.y / 2, fallbackSize.x, fallbackSize.y);
                CarveRoom(grid, fallbackRoom);
                rooms.Add(fallbackRoom);
            }

            var startCell = GetCenter(rooms[0]);
            EnsureSafeRadius(grid, startCell, _config.SafeRadius);
            grid.SetBorderWalls();

            return new MapGenerationResult(grid, startCell, _config.SafeRadius);
        }

        private void CarveRoom(MapGrid grid, RectInt room)
        {
            for (var y = room.yMin; y < room.yMax; y++)
            {
                for (var x = room.xMin; x < room.xMax; x++)
                {
                    grid.Set(x, y, CellType.Floor);
                }
            }
        }

        private void CarveCorridor(MapGrid grid, Vector2Int from, Vector2Int to, System.Random rng)
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

        private void CarveHorizontal(MapGrid grid, int x0, int x1, int y)
        {
            var start = Mathf.Min(x0, x1);
            var end = Mathf.Max(x0, x1);
            for (var x = start; x <= end; x++)
            {
                grid.Set(x, y, CellType.Floor);
            }
        }

        private void CarveVertical(MapGrid grid, int y0, int y1, int x)
        {
            var start = Mathf.Min(y0, y1);
            var end = Mathf.Max(y0, y1);
            for (var y = start; y <= end; y++)
            {
                grid.Set(x, y, CellType.Floor);
            }
        }

        private Vector2Int GetCenter(RectInt rect)
        {
            return new Vector2Int(rect.x + rect.width / 2, rect.y + rect.height / 2);
        }

        private bool IsOverlapping(RectInt room, List<RectInt> rooms)
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

        private void EnsureSafeRadius(MapGrid grid, Vector2Int center, int radius)
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
