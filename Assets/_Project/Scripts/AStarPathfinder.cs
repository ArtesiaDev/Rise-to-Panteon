using System.Collections.Generic;
using UnityEngine;

namespace RuntimeRoguelike
{
    public sealed class AStarPathfinder
    {
        private readonly MapGrid grid;
        private readonly GridOccupancy occupancy;

        public AStarPathfinder(MapGrid grid, GridOccupancy occupancy)
        {
            this.grid = grid;
            this.occupancy = occupancy;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            var result = new List<Vector2Int>();
            if (!grid.InBounds(start) || !grid.InBounds(goal))
            {
                return result;
            }

            if (!grid.IsWalkable(goal))
            {
                return result;
            }

            if (start == goal)
            {
                return result;
            }

            var total = grid.Width * grid.Height;
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
                    if (!grid.InBounds(neighbor))
                    {
                        continue;
                    }

                    if (!grid.IsWalkable(neighbor))
                    {
                        continue;
                    }

                    if (neighbor != goal && occupancy.IsOccupied(neighbor))
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
            return cell.y * grid.Width + cell.x;
        }

        private Vector2Int ToCell(int index)
        {
            var x = index % grid.Width;
            var y = index / grid.Width;
            return new Vector2Int(x, y);
        }

        private static int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static Vector2Int Direction(int index)
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

        private sealed class MinHeap
        {
            private readonly int[] heap;
            private readonly int[] positions;
            private readonly int[] fScore;
            private int count;

            public int Count => count;

            public MinHeap(int capacity, int[] fScore)
            {
                heap = new int[capacity];
                positions = new int[capacity];
                this.fScore = fScore;
                count = 0;
                for (var i = 0; i < positions.Length; i++)
                {
                    positions[i] = -1;
                }
            }

            public bool Contains(int index)
            {
                return positions[index] >= 0;
            }

            public void Push(int index)
            {
                heap[count] = index;
                positions[index] = count;
                BubbleUp(count);
                count++;
            }

            public int Pop()
            {
                var min = heap[0];
                count--;
                if (count > 0)
                {
                    heap[0] = heap[count];
                    positions[heap[0]] = 0;
                    BubbleDown(0);
                }

                positions[min] = -1;
                return min;
            }

            public void Update(int index)
            {
                var position = positions[index];
                if (position < 0)
                {
                    return;
                }

                if (!BubbleUp(position))
                {
                    BubbleDown(position);
                }
            }

            private bool BubbleUp(int index)
            {
                var moved = false;
                while (index > 0)
                {
                    var parent = (index - 1) / 2;
                    if (Compare(heap[index], heap[parent]) >= 0)
                    {
                        break;
                    }

                    Swap(index, parent);
                    index = parent;
                    moved = true;
                }

                return moved;
            }

            private void BubbleDown(int index)
            {
                while (true)
                {
                    var left = index * 2 + 1;
                    if (left >= count)
                    {
                        return;
                    }

                    var right = left + 1;
                    var smallest = left;
                    if (right < count && Compare(heap[right], heap[left]) < 0)
                    {
                        smallest = right;
                    }

                    if (Compare(heap[index], heap[smallest]) <= 0)
                    {
                        return;
                    }

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private int Compare(int a, int b)
            {
                var fA = fScore[a];
                var fB = fScore[b];
                if (fA != fB)
                {
                    return fA < fB ? -1 : 1;
                }

                return a < b ? -1 : 1;
            }

            private void Swap(int a, int b)
            {
                var temp = heap[a];
                heap[a] = heap[b];
                heap[b] = temp;
                positions[heap[a]] = a;
                positions[heap[b]] = b;
            }
        }
    }
}
