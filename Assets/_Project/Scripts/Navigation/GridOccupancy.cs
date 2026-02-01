using UnityEngine;
using RuntimeRoguelike.Ecs;

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

        public bool IsOccupied(Int2 cell)
        {
            return IsOccupied(cell.X, cell.Y);
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

        public bool TryOccupy(Int2 cell)
        {
            return TryOccupy(new Vector2Int(cell.X, cell.Y));
        }

        public void Occupy(Vector2Int cell)
        {
            if (InBounds(cell.x, cell.y))
            {
                _occupied[ToIndex(cell.x, cell.y)] = true;
            }
        }

        public void Occupy(Int2 cell)
        {
            Occupy(new Vector2Int(cell.X, cell.Y));
        }

        public void Release(Vector2Int cell)
        {
            if (InBounds(cell.x, cell.y))
            {
                _occupied[ToIndex(cell.x, cell.y)] = false;
            }
        }

        public void Release(Int2 cell)
        {
            Release(new Vector2Int(cell.X, cell.Y));
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

        public bool TryMove(Int2 from, Int2 to)
        {
            return TryMove(new Vector2Int(from.X, from.Y), new Vector2Int(to.X, to.Y));
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
