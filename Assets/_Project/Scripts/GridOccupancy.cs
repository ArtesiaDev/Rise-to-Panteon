using UnityEngine;

namespace RuntimeRoguelike
{
    public sealed class GridOccupancy
    {
        private readonly int width;
        private readonly int height;
        private readonly bool[] occupied;

        public GridOccupancy(int width, int height)
        {
            this.width = width;
            this.height = height;
            occupied = new bool[width * height];
        }

        public bool IsOccupied(Vector2Int cell)
        {
            return IsOccupied(cell.x, cell.y);
        }

        public bool IsOccupied(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return true;
            }

            return occupied[ToIndex(x, y)];
        }

        public bool TryOccupy(Vector2Int cell)
        {
            if (!InBounds(cell.x, cell.y))
            {
                return false;
            }

            var index = ToIndex(cell.x, cell.y);
            if (occupied[index])
            {
                return false;
            }

            occupied[index] = true;
            return true;
        }

        public void Occupy(Vector2Int cell)
        {
            if (InBounds(cell.x, cell.y))
            {
                occupied[ToIndex(cell.x, cell.y)] = true;
            }
        }

        public void Release(Vector2Int cell)
        {
            if (InBounds(cell.x, cell.y))
            {
                occupied[ToIndex(cell.x, cell.y)] = false;
            }
        }

        public bool TryMove(Vector2Int from, Vector2Int to)
        {
            if (!InBounds(to.x, to.y))
            {
                return false;
            }

            var toIndex = ToIndex(to.x, to.y);
            if (occupied[toIndex])
            {
                return false;
            }

            if (InBounds(from.x, from.y))
            {
                occupied[ToIndex(from.x, from.y)] = false;
            }

            occupied[toIndex] = true;
            return true;
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        private int ToIndex(int x, int y)
        {
            return y * width + x;
        }
    }
}
