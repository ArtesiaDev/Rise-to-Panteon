using UnityEngine;

namespace RuntimeRoguelike
{
    public class GridOccupancy
    {
        private readonly int _width;
        private readonly int _height;
        private readonly bool[] _occupied;

        public GridOccupancy(int width, int height)
        {
            _width = width;
            _height = height;
            _occupied = new bool[width * height];
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

            return _occupied[ToIndex(x, y)];
        }

        public bool TryOccupy(Vector2Int cell)
        {
            if (!InBounds(cell.x, cell.y))
            {
                return false;
            }

            var index = ToIndex(cell.x, cell.y);
            if (_occupied[index])
            {
                return false;
            }

            _occupied[index] = true;
            return true;
        }

        public void Occupy(Vector2Int cell)
        {
            if (InBounds(cell.x, cell.y))
            {
                _occupied[ToIndex(cell.x, cell.y)] = true;
            }
        }

        public void Release(Vector2Int cell)
        {
            if (InBounds(cell.x, cell.y))
            {
                _occupied[ToIndex(cell.x, cell.y)] = false;
            }
        }

        public bool TryMove(Vector2Int from, Vector2Int to)
        {
            if (!InBounds(to.x, to.y))
            {
                return false;
            }

            var toIndex = ToIndex(to.x, to.y);
            if (_occupied[toIndex])
            {
                return false;
            }

            if (InBounds(from.x, from.y))
            {
                _occupied[ToIndex(from.x, from.y)] = false;
            }

            _occupied[toIndex] = true;
            return true;
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _width && y < _height;
        }

        private int ToIndex(int x, int y)
        {
            return y * _width + x;
        }
    }
}
