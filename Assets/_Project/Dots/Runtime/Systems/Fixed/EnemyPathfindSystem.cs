using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyTargetAcquireSystem))]
    public partial struct EnemyPathfindSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MapBlobReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var mapRef = SystemAPI.GetSingleton<MapBlobReference>();
            if (!mapRef.Value.IsCreated)
            {
                return;
            }

            var map = mapRef.Value.Value;
            var occupancy = state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>());

            foreach (var (position, target, refresh, path, index) in SystemAPI.Query<RefRO<GridPosition>, RefRW<Target>, RefRW<PathRefreshCooldown>, DynamicBuffer<PathStep>, RefRW<PathIndex>>().WithAll<EnemyTag>())
            {
                if (!target.ValueRO.HasTarget || !state.EntityManager.Exists(target.ValueRO.Value))
                {
                    target.ValueRW.HasTarget = false;
                    path.Clear();
                    index.ValueRW.Value = 0;
                    continue;
                }

                if (refresh.ValueRO.Remaining > 0f)
                {
                    continue;
                }

                var start = position.ValueRO.Value;
                var goal = SystemAPI.GetComponent<GridPosition>(target.ValueRO.Value).Value;

                using var pathResult = new NativeList<int2>(Allocator.Temp);
                FindPath(map, occupancy, start, goal, ref pathResult);

                path.Clear();
                for (var i = 0; i < pathResult.Length; i++)
                {
                    path.Add(new PathStep { Value = pathResult[i] });
                }

                index.ValueRW.Value = 0;
                refresh.ValueRW.Remaining = refresh.ValueRO.Interval;
            }
        }

        private static void FindPath(in MapBlob map, DynamicBuffer<CellOccupant> occupancy, int2 start, int2 goal, ref NativeList<int2> result)
        {
            result.Clear();

            if (!MapUtilities.InBounds(start, map.Size) || !MapUtilities.InBounds(goal, map.Size))
            {
                return;
            }

            if (!MapUtilities.IsWalkable(map, goal) || start.Equals(goal))
            {
                return;
            }

            var total = map.Size.x * map.Size.y;
            using var gScore = new NativeArray<int>(total, Allocator.Temp);
            using var fScore = new NativeArray<int>(total, Allocator.Temp);
            using var cameFrom = new NativeArray<int>(total, Allocator.Temp);
            using var closed = new NativeArray<byte>(total, Allocator.Temp);
            using var heapArray = new NativeArray<int>(total, Allocator.Temp);
            using var positions = new NativeArray<int>(total, Allocator.Temp);

            for (var i = 0; i < total; i++)
            {
                gScore[i] = int.MaxValue;
                fScore[i] = int.MaxValue;
                cameFrom[i] = -1;
                closed[i] = 0;
                positions[i] = -1;
            }

            var startIndex = MapUtilities.ToIndex(start, map.Size);
            var goalIndex = MapUtilities.ToIndex(goal, map.Size);

            gScore[startIndex] = 0;
            fScore[startIndex] = Heuristic(start, goal);

            var openSet = new MinHeap(heapArray, positions, fScore);
            openSet.Push(startIndex);

            while (openSet.Count > 0)
            {
                var current = openSet.Pop();
                if (current == goalIndex)
                {
                    ReconstructPath(cameFrom, current, startIndex, map.Size, ref result);
                    return;
                }

                closed[current] = 1;
                var currentCell = ToCell(current, map.Size);

                for (var i = 0; i < 4; i++)
                {
                    var neighbor = currentCell + Direction(i);
                    if (!MapUtilities.InBounds(neighbor, map.Size))
                    {
                        continue;
                    }

                    if (!MapUtilities.IsWalkable(map, neighbor))
                    {
                        continue;
                    }

                    var neighborIndex = MapUtilities.ToIndex(neighbor, map.Size);
                    if (neighborIndex != goalIndex && occupancy[neighborIndex].Value != Entity.Null)
                    {
                        continue;
                    }

                    if (closed[neighborIndex] != 0)
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
        }

        private static void ReconstructPath(NativeArray<int> cameFrom, int currentIndex, int startIndex, int2 size, ref NativeList<int2> result)
        {
            var index = currentIndex;
            while (index != -1 && index != startIndex)
            {
                result.Add(ToCell(index, size));
                index = cameFrom[index];
            }

            for (var i = 0; i < result.Length / 2; i++)
            {
                var temp = result[i];
                result[i] = result[result.Length - 1 - i];
                result[result.Length - 1 - i] = temp;
            }
        }

        private static int2 ToCell(int index, int2 size)
        {
            var x = index % size.x;
            var y = index / size.x;
            return new int2(x, y);
        }

        private static int Heuristic(int2 a, int2 b)
        {
            return math.abs(a.x - b.x) + math.abs(a.y - b.y);
        }

        private static int2 Direction(int index)
        {
            return index switch
            {
                0 => new int2(0, 1),
                1 => new int2(1, 0),
                2 => new int2(0, -1),
                _ => new int2(-1, 0)
            };
        }

        private struct MinHeap
        {
            private NativeArray<int> _heap;
            private NativeArray<int> _positions;
            private NativeArray<int> _fScore;
            private int _count;

            public int Count => _count;

            public MinHeap(NativeArray<int> heap, NativeArray<int> positions, NativeArray<int> fScore)
            {
                _heap = heap;
                _positions = positions;
                _fScore = fScore;
                _count = 0;
            }

            public bool Contains(int index)
            {
                return _positions[index] >= 0;
            }

            public void Push(int index)
            {
                _heap[_count] = index;
                _positions[index] = _count;
                BubbleUp(_count);
                _count++;
            }

            public int Pop()
            {
                var min = _heap[0];
                _count--;
                if (_count > 0)
                {
                    _heap[0] = _heap[_count];
                    _positions[_heap[0]] = 0;
                    BubbleDown(0);
                }

                _positions[min] = -1;
                return min;
            }

            public void Update(int index)
            {
                var position = _positions[index];
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
                    if (Compare(_heap[index], _heap[parent]) >= 0)
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
                    if (left >= _count)
                    {
                        return;
                    }

                    var right = left + 1;
                    var smallest = left;
                    if (right < _count && Compare(_heap[right], _heap[left]) < 0)
                    {
                        smallest = right;
                    }

                    if (Compare(_heap[index], _heap[smallest]) <= 0)
                    {
                        return;
                    }

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private int Compare(int a, int b)
            {
                var fA = _fScore[a];
                var fB = _fScore[b];
                if (fA != fB)
                {
                    return fA < fB ? -1 : 1;
                }

                return a < b ? -1 : 1;
            }

            private void Swap(int a, int b)
            {
                var temp = _heap[a];
                _heap[a] = _heap[b];
                _heap[b] = temp;
                _positions[_heap[a]] = a;
                _positions[_heap[b]] = b;
            }
        }
    }
}
