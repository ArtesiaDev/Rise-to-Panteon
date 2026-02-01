using UnityEngine;
using RuntimeRoguelike.Ecs;

namespace RuntimeRoguelike
{
    public class MapGrid
    {
        private readonly CellType[] _baseLayer;
        private readonly ObstacleType[] _obstacleLayer;
        private readonly HazardType[] _hazardLayer;

        public int Width { get; }
        public int Height { get; }

        public MapGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _baseLayer = new CellType[width * height];
            _obstacleLayer = new ObstacleType[width * height];
            _hazardLayer = new HazardType[width * height];
            FillBase(CellType.Empty);
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public bool InBounds(Vector2Int cell)
        {
            return InBounds(cell.x, cell.y);
        }

        public bool InBounds(Int2 cell)
        {
            return InBounds(cell.X, cell.Y);
        }

        public MapCell Get(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return new MapCell(CellType.Empty, ObstacleType.None, HazardType.None, false, true, 0);
            }

            var index = ToIndex(x, y);
            var baseType = _baseLayer[index];
            var obstacle = _obstacleLayer[index];
            var hazard = _hazardLayer[index];
            var walkable = baseType == CellType.Floor && obstacle == ObstacleType.None;
            var blocksVision = baseType == CellType.Wall || obstacle != ObstacleType.None;
            var onEnterDamage = hazard == HazardType.Spike ? 1 : 0;

            return new MapCell(baseType, obstacle, hazard, walkable, blocksVision, onEnterDamage);
        }

        public MapCell Get(Vector2Int cell)
        {
            return Get(cell.x, cell.y);
        }

        public MapCell Get(Int2 cell)
        {
            return Get(cell.X, cell.Y);
        }

        public void Set(int x, int y, CellType type)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            _baseLayer[ToIndex(x, y)] = type;
        }

        public void Set(Vector2Int cell, CellType type)
        {
            Set(cell.x, cell.y, type);
        }

        public void Set(Int2 cell, CellType type)
        {
            Set(cell.X, cell.Y, type);
        }

        public void SetObstacle(int x, int y, ObstacleType type)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            _obstacleLayer[ToIndex(x, y)] = type;
        }

        public void SetObstacle(Vector2Int cell, ObstacleType type)
        {
            SetObstacle(cell.x, cell.y, type);
        }

        public void SetObstacle(Int2 cell, ObstacleType type)
        {
            SetObstacle(cell.X, cell.Y, type);
        }

        public void SetHazard(int x, int y, HazardType type)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            _hazardLayer[ToIndex(x, y)] = type;
        }

        public void SetHazard(Vector2Int cell, HazardType type)
        {
            SetHazard(cell.x, cell.y, type);
        }

        public void SetHazard(Int2 cell, HazardType type)
        {
            SetHazard(cell.X, cell.Y, type);
        }

        public bool IsWalkable(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return false;
            }

            var index = ToIndex(x, y);
            return _baseLayer[index] == CellType.Floor && _obstacleLayer[index] == ObstacleType.None;
        }

        public bool IsWalkable(Vector2Int cell)
        {
            return IsWalkable(cell.x, cell.y);
        }

        public bool IsWalkable(Int2 cell)
        {
            return IsWalkable(cell.X, cell.Y);
        }

        public void FillBase(CellType type)
        {
            for (var i = 0; i < _baseLayer.Length; i++)
            {
                _baseLayer[i] = type;
            }
        }

        public void ClearObstacles()
        {
            for (var i = 0; i < _obstacleLayer.Length; i++)
            {
                _obstacleLayer[i] = ObstacleType.None;
            }
        }

        public void ClearHazards()
        {
            for (var i = 0; i < _hazardLayer.Length; i++)
            {
                _hazardLayer[i] = HazardType.None;
            }
        }

        public void SetBorderWalls()
        {
            for (var x = 0; x < Width; x++)
            {
                Set(x, 0, CellType.Wall);
                Set(x, Height - 1, CellType.Wall);
            }

            for (var y = 0; y < Height; y++)
            {
                Set(0, y, CellType.Wall);
                Set(Width - 1, y, CellType.Wall);
            }
        }

        private int ToIndex(int x, int y)
        {
            return y * Width + x;
        }
    }
}
