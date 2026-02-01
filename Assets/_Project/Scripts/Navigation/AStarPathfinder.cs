using System.Collections.Generic;
using UnityEngine;

namespace RuntimeRoguelike
{
    public class AStarPathfinder
    {
        private readonly MapGrid _grid;
        private readonly GridOccupancy _occupancy;

        public AStarPathfinder(MapGrid grid, GridOccupancy occupancy)
        {
            _grid = grid;
            _occupancy = occupancy;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            var result = new List<Vector2Int>();
            if (!_grid.InBounds(start) || !_grid.InBounds(goal))
            {
                return result;
            }

            if (!_grid.IsWalkable(goal))
            {
                return result;
            }

            if (start == goal)
            {
                return result;
            }

            var total = _grid.Width * _grid.Height;
            var gScore = new int[total];
            var fScore = new int[total];
            var cameFrom = new int[total];
            var closed = new bool[total];

            for (var i = 0; i < total; i++)
            {
                gScore[i] = int.MaxValue;
                fScore[i] = int.MaxValue;
                cameFrom[i] = -1;
            }

            var openSet = new MinHeap(total, fScore);
            var startIndex = ToIndex(start);
            var goalIndex = ToIndex(goal);
            gScore[startIndex] = 0;
            fScore[startIndex] = Heuristic(start, goal);
            openSet.Push(startIndex);

            while (openSet.Count > 0)
            {
                var current = openSet.Pop();
                if (current == goalIndex)
                {
                    return ReconstructPath(cameFrom, current, startIndex);
                }

                closed[current] = true;
                var currentCell = ToCell(current);

                for (var i = 0; i < 4; i++)
                {
                    var neighbor = currentCell + Direction(i);
                    if (!_grid.InBounds(neighbor))
                    {
                        continue;
                    }

                    if (!_grid.IsWalkable(neighbor))
                    {
                        continue;
                    }

                    if (neighbor != goal && _occupancy.IsOccupied(neighbor))
                    {
                        continue;
                    }

                    var neighborIndex = ToIndex(neighbor);
                    if (closed[neighborIndex])
                    {
                        continue;
                    }

                    var tentativeG = gScore[current] + 1;
                    if (tentativeG >= gScore[neighborIndex])
                    {
                        continue;
                    }

                    cameFrom[neighborIndex] = current;
                    gScore[neighborIndex] = tentativeG;
                    fScore[neighborIndex] = tentativeG + Heuristic(neighbor, goal);

                    if (openSet.Contains(neighborIndex))
                    {
                        openSet.Update(neighborIndex);
                    }
                    else
                    {
                        openSet.Push(neighborIndex);
                    }
                }
            }

            return result;
        }

        private List<Vector2Int> ReconstructPath(int[] cameFrom, int currentIndex, int startIndex)
        {
            var path = new List<Vector2Int>();
            var index = currentIndex;
            while (index != -1 && index != startIndex)
            {
                path.Add(ToCell(index));
                index = cameFrom[index];
            }

            path.Reverse();
            return path;
        }

        private int ToIndex(Vector2Int cell)
        {
            return cell.y * _grid.Width + cell.x;
        }

        private Vector2Int ToCell(int index)
        {
            var x = index % _grid.Width;
            var y = index / _grid.Width;
            return new Vector2Int(x, y);
        }

        private int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private Vector2Int Direction(int index)
        {
            switch (index)
            {
                case 0:
                    return Vector2Int.up;
                case 1:
                    return Vector2Int.right;
                case 2:
                    return Vector2Int.down;
                default:
                    return Vector2Int.left;
            }
        }
    }
}
