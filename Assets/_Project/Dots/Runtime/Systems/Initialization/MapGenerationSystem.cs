using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace RuntimeRoguelike.Dots.Runtime
{
    // [BurstCompile] невозможен — система использует EntityManager напрямую (BlobBuilder, AddComponent).
    // Внутренние статические методы (CarveRoom, CarveCorridor и т.д.) Burst-совместимы.
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct MapGenerationSystem : ISystem
    {
        private struct RoomRect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;

            public int XMin => X;
            public int YMin => Y;
            public int XMax => X + Width;
            public int YMax => Y + Height;
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<RunConfigData>();
            state.RequireForUpdate<MapGenerationConfigData>();
            state.RequireForUpdate<HazardConfigData>();
            state.RequireForUpdate<RngState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var runState = SystemAPI.GetSingletonRW<RunState>();
            if (runState.ValueRO.IsInitialized)
            {
                return;
            }

            var runConfig = SystemAPI.GetSingleton<RunConfigData>();
            var mapConfig = SystemAPI.GetSingleton<MapGenerationConfigData>();
            var hazardConfig = SystemAPI.GetSingleton<HazardConfigData>();
            var rngState = SystemAPI.GetSingletonRW<RngState>();

            var mapSize = new int2(math.max(10, runConfig.MapSize.x), math.max(10, runConfig.MapSize.y));
            var cellCount = mapSize.x * mapSize.y;

            if (!rngState.ValueRO.IsInitialized)
            {
                uint seed = runConfig.RandomizeSeedOnStart
                    ? Random.CreateFromIndex((uint)state.WorldUnmanaged.Time.ElapsedTime * 1000u + 1u).NextUInt(1u, uint.MaxValue)
                    : (uint)runConfig.InitialSeed;
                if (seed == 0)
                {
                    seed = 1;
                }

                rngState.ValueRW.Rng = Random.CreateFromIndex(seed);
                rngState.ValueRW.IsInitialized = true;
                runState.ValueRW.Seed = seed;
            }

            var rng = rngState.ValueRW.Rng;

            var baseLayer = new NativeArray<MapCellType>(cellCount, Allocator.Temp);
            var obstacleLayer = new NativeArray<ObstacleType>(cellCount, Allocator.Temp);
            var hazardLayer = new NativeArray<HazardType>(cellCount, Allocator.Temp);
            var rooms = new NativeList<RoomRect>(mapConfig.RoomAttempts, Allocator.Temp);

            try
            {
                for (var i = 0; i < cellCount; i++)
                {
                    baseLayer[i] = MapCellType.Wall;
                    obstacleLayer[i] = ObstacleType.None;
                    hazardLayer[i] = HazardType.None;
                }

                for (var i = 0; i < mapConfig.RoomAttempts; i++)
                {
                    var roomWidth = rng.NextInt(mapConfig.MinRoomSize, mapConfig.MaxRoomSize + 1);
                    var roomHeight = rng.NextInt(mapConfig.MinRoomSize, mapConfig.MaxRoomSize + 1);

                    if (mapSize.x - roomWidth - 1 <= 1 || mapSize.y - roomHeight - 1 <= 1)
                    {
                        continue;
                    }

                    var roomX = rng.NextInt(1, mapSize.x - roomWidth - 1);
                    var roomY = rng.NextInt(1, mapSize.y - roomHeight - 1);
                    var room = new RoomRect { X = roomX, Y = roomY, Width = roomWidth, Height = roomHeight };

                    if (IsOverlapping(room, ref rooms))
                    {
                        continue;
                    }

                    CarveRoom(baseLayer, mapSize, room);

                    if (rooms.Length > 0)
                    {
                        var previousCenter = GetCenter(rooms[rooms.Length - 1]);
                        var currentCenter = GetCenter(room);
                        CarveCorridor(baseLayer, mapSize, previousCenter, currentCenter, ref rng);
                    }

                    rooms.Add(room);
                }

                if (rooms.Length == 0)
                {
                    var fallback = mapConfig.FallbackRoomSize;
                    var fallbackRoom = new RoomRect
                    {
                        X = mapSize.x / 2 - fallback.x / 2,
                        Y = mapSize.y / 2 - fallback.y / 2,
                        Width = fallback.x,
                        Height = fallback.y
                    };
                    CarveRoom(baseLayer, mapSize, fallbackRoom);
                    rooms.Add(fallbackRoom);
                }

                var startCell = GetCenter(rooms[0]);
                EnsureSafeRadius(baseLayer, obstacleLayer, hazardLayer, mapSize, startCell, mapConfig.SafeRadius);
                SetBorderWalls(baseLayer, mapSize);
                PopulateHazards(hazardLayer, baseLayer, obstacleLayer, mapSize, startCell, mapConfig.SafeRadius, hazardConfig, runState.ValueRO.Seed);

                rngState.ValueRW.Rng = rng;

                var mapEntity = SystemAPI.GetSingletonEntity<RunState>();
                if (state.EntityManager.HasComponent<MapBlobReference>(mapEntity))
                {
                    var existing = state.EntityManager.GetComponentData<MapBlobReference>(mapEntity);
                    if (existing.Value.IsCreated)
                    {
                        existing.Value.Dispose();
                    }
                }

                using var builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<MapBlob>();
                root.Size = mapSize;
                root.StartCell = startCell;

                var baseBlob = builder.Allocate(ref root.BaseLayer, cellCount);
                var obstacleBlob = builder.Allocate(ref root.ObstacleLayer, cellCount);
                var hazardBlob = builder.Allocate(ref root.HazardLayer, cellCount);

                for (var i = 0; i < cellCount; i++)
                {
                    baseBlob[i] = baseLayer[i];
                    obstacleBlob[i] = obstacleLayer[i];
                    hazardBlob[i] = hazardLayer[i];
                }

                var mapBlobRef = builder.CreateBlobAssetReference<MapBlob>(Allocator.Persistent);

                if (state.EntityManager.HasComponent<MapBlobReference>(mapEntity))
                {
                    state.EntityManager.SetComponentData(mapEntity, new MapBlobReference { Value = mapBlobRef });
                }
                else
                {
                    state.EntityManager.AddComponentData(mapEntity, new MapBlobReference { Value = mapBlobRef });
                }

                if (!state.EntityManager.HasComponent<MapRenderRequest>(mapEntity))
                {
                    state.EntityManager.AddComponentData(mapEntity, new MapRenderRequest { RunId = runState.ValueRO.RunId });
                }
                else
                {
                    state.EntityManager.SetComponentData(mapEntity, new MapRenderRequest { RunId = runState.ValueRO.RunId });
                }
                state.EntityManager.SetComponentEnabled<MapRenderRequest>(mapEntity, true);

                var occupancy = state.EntityManager.HasComponent<CellOccupant>(mapEntity)
                    ? state.EntityManager.GetBuffer<CellOccupant>(mapEntity)
                    : state.EntityManager.AddBuffer<CellOccupant>(mapEntity);

                occupancy.Clear();
                occupancy.ResizeUninitialized(cellCount);
                for (var i = 0; i < cellCount; i++)
                {
                    occupancy[i] = new CellOccupant { Value = Entity.Null };
                }

                runState.ValueRW.MapSize = mapSize;
                runState.ValueRW.StartCell = startCell;
                runState.ValueRW.SafeRadius = mapConfig.SafeRadius;
                runState.ValueRW.IsInitialized = true;
            }
            finally
            {
                baseLayer.Dispose();
                obstacleLayer.Dispose();
                hazardLayer.Dispose();
                rooms.Dispose();
            }
        }

        private static void CarveRoom(NativeArray<MapCellType> baseLayer, int2 size, RoomRect room)
        {
            for (var y = room.YMin; y < room.YMax; y++)
            {
                for (var x = room.XMin; x < room.XMax; x++)
                {
                    var index = y * size.x + x;
                    baseLayer[index] = MapCellType.Floor;
                }
            }
        }

        private static void CarveCorridor(NativeArray<MapCellType> baseLayer, int2 size, int2 from, int2 to, ref Random rng)
        {
            if (rng.NextFloat() < 0.5f)
            {
                CarveHorizontal(baseLayer, size, from.x, to.x, from.y);
                CarveVertical(baseLayer, size, from.y, to.y, to.x);
            }
            else
            {
                CarveVertical(baseLayer, size, from.y, to.y, from.x);
                CarveHorizontal(baseLayer, size, from.x, to.x, to.y);
            }
        }

        private static void CarveHorizontal(NativeArray<MapCellType> baseLayer, int2 size, int x0, int x1, int y)
        {
            var start = math.min(x0, x1);
            var end = math.max(x0, x1);
            for (var x = start; x <= end; x++)
            {
                baseLayer[y * size.x + x] = MapCellType.Floor;
            }
        }

        private static void CarveVertical(NativeArray<MapCellType> baseLayer, int2 size, int y0, int y1, int x)
        {
            var start = math.min(y0, y1);
            var end = math.max(y0, y1);
            for (var y = start; y <= end; y++)
            {
                baseLayer[y * size.x + x] = MapCellType.Floor;
            }
        }

        private static int2 GetCenter(RoomRect room)
        {
            return new int2(room.X + room.Width / 2, room.Y + room.Height / 2);
        }

        private static bool IsOverlapping(RoomRect room, ref NativeList<RoomRect> rooms)
        {
            var expanded = new RoomRect
            {
                X = room.X - 1,
                Y = room.Y - 1,
                Width = room.Width + 2,
                Height = room.Height + 2
            };

            for (var i = 0; i < rooms.Length; i++)
            {
                if (Overlaps(expanded, rooms[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Overlaps(RoomRect a, RoomRect b)
        {
            return a.XMin < b.XMax && a.XMax > b.XMin && a.YMin < b.YMax && a.YMax > b.YMin;
        }

        private static void EnsureSafeRadius(NativeArray<MapCellType> baseLayer, NativeArray<ObstacleType> obstacleLayer, NativeArray<HazardType> hazardLayer, int2 size, int2 center, int radius)
        {
            for (var y = center.y - radius; y <= center.y + radius; y++)
            {
                for (var x = center.x - radius; x <= center.x + radius; x++)
                {
                    var cell = new int2(x, y);
                    if (!MapUtilities.InBounds(cell, size))
                    {
                        continue;
                    }

                    var dx = x - center.x;
                    var dy = y - center.y;
                    if (dx * dx + dy * dy > radius * radius)
                    {
                        continue;
                    }

                    var index = y * size.x + x;
                    baseLayer[index] = MapCellType.Floor;
                    obstacleLayer[index] = ObstacleType.None;
                    hazardLayer[index] = HazardType.None;
                }
            }
        }

        private static void SetBorderWalls(NativeArray<MapCellType> baseLayer, int2 size)
        {
            for (var x = 0; x < size.x; x++)
            {
                baseLayer[x] = MapCellType.Wall;
                baseLayer[(size.y - 1) * size.x + x] = MapCellType.Wall;
            }

            for (var y = 0; y < size.y; y++)
            {
                baseLayer[y * size.x] = MapCellType.Wall;
                baseLayer[y * size.x + (size.x - 1)] = MapCellType.Wall;
            }
        }

        private static void PopulateHazards(NativeArray<HazardType> hazardLayer, NativeArray<MapCellType> baseLayer, NativeArray<ObstacleType> obstacleLayer, int2 size, int2 startCell, int safeRadius, HazardConfigData hazardConfig, uint baseSeed)
        {
            var hazardSeed = baseSeed + 4444u;
            if (hazardSeed == 0)
            {
                hazardSeed = 1;
            }

            var rng = Random.CreateFromIndex(hazardSeed);

            for (var y = 1; y < size.y - 1; y++)
            {
                for (var x = 1; x < size.x - 1; x++)
                {
                    var dx = x - startCell.x;
                    var dy = y - startCell.y;
                    if (dx * dx + dy * dy <= safeRadius * safeRadius)
                    {
                        continue;
                    }

                    var index = y * size.x + x;
                    if (baseLayer[index] != MapCellType.Floor || obstacleLayer[index] != ObstacleType.None)
                    {
                        continue;
                    }

                    if (rng.NextFloat() >= hazardConfig.HazardChance)
                    {
                        continue;
                    }

                    var type = rng.NextFloat() < hazardConfig.SpikeChance ? HazardType.Spike : HazardType.Poison;
                    hazardLayer[index] = type;
                }
            }
        }
    }
}
