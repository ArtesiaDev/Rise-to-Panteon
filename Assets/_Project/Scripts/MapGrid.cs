using UnityEngine;

namespace RuntimeRoguelike
{
    public enum CellType
    {
        Empty,
        Floor,
        Wall,
        Obstacle,
        Hazard
    }

    public enum ObstacleType
    {
        None,
        Rock
    }

    public enum HazardType
    {
        None,
        Spike,
        Poison
    }

    public readonly struct MapCell
    {
        public readonly CellType cellType;
        public readonly ObstacleType obstacle;
        public readonly HazardType hazard;
        public readonly bool walkable;
        public readonly bool blocksVision;
        public readonly int onEnterDamage;

        public MapCell(CellType cellType, ObstacleType obstacle, HazardType hazard, bool walkable, bool blocksVision, int onEnterDamage)
        {
            this.cellType = cellType;
            this.obstacle = obstacle;
            this.hazard = hazard;
            this.walkable = walkable;
            this.blocksVision = blocksVision;
            this.onEnterDamage = onEnterDamage;
        }
    }

    public sealed class MapGrid
    {
        private readonly CellType[] baseLayer;
        private readonly ObstacleType[] obstacleLayer;
        private readonly HazardType[] hazardLayer;

        public int Width { get; }
        public int Height { get; }

        public MapGrid(int width, int height)
        {
            Width = width;
            Height = height;
            baseLayer = new CellType[width * height];
            obstacleLayer = new ObstacleType[width * height];
            hazardLayer = new HazardType[width * height];
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

        public MapCell Get(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return new MapCell(CellType.Empty, ObstacleType.None, HazardType.None, false, true, 0);
            }

            var index = ToIndex(x, y);
            var baseType = baseLayer[index];
            var obstacle = obstacleLayer[index];
            var hazard = hazardLayer[index];
            var walkable = baseType == CellType.Floor && obstacle == ObstacleType.None;
            var blocksVision = baseType == CellType.Wall || obstacle != ObstacleType.None;
            var onEnterDamage = hazard == HazardType.Spike ? 1 : 0;

            return new MapCell(baseType, obstacle, hazard, walkable, blocksVision, onEnterDamage);
        }

        public MapCell Get(Vector2Int cell)
        {
            return Get(cell.x, cell.y);
        }

        public void Set(int x, int y, CellType type)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            baseLayer[ToIndex(x, y)] = type;
        }

        public void Set(Vector2Int cell, CellType type)
        {
            Set(cell.x, cell.y, type);
        }

        public void SetObstacle(int x, int y, ObstacleType type)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            obstacleLayer[ToIndex(x, y)] = type;
        }

        public void SetObstacle(Vector2Int cell, ObstacleType type)
        {
            SetObstacle(cell.x, cell.y, type);
        }

        public void SetHazard(int x, int y, HazardType type)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            hazardLayer[ToIndex(x, y)] = type;
        }

        public void SetHazard(Vector2Int cell, HazardType type)
        {
            SetHazard(cell.x, cell.y, type);
        }

        public bool IsWalkable(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return false;
            }

            var index = ToIndex(x, y);
            return baseLayer[index] == CellType.Floor && obstacleLayer[index] == ObstacleType.None;
        }

        public bool IsWalkable(Vector2Int cell)
        {
            return IsWalkable(cell.x, cell.y);
        }

        public void FillBase(CellType type)
        {
            for (var i = 0; i < baseLayer.Length; i++)
            {
                baseLayer[i] = type;
            }
        }

        public void ClearObstacles()
        {
            for (var i = 0; i < obstacleLayer.Length; i++)
            {
                obstacleLayer[i] = ObstacleType.None;
            }
        }

        public void ClearHazards()
        {
            for (var i = 0; i < hazardLayer.Length; i++)
            {
                hazardLayer[i] = HazardType.None;
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
