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

        // Ребро графа комнат для MST
        private struct RoomEdge
        {
            public int RoomA;
            public int RoomB;
            public float Distance;
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

            var mapSize = new int2(math.max(10, mapConfig.MapSize.x), math.max(10, mapConfig.MapSize.y));
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
            var wallMaskLayer = new NativeArray<byte>(cellCount, Allocator.Temp);
            var floorVariantLayer = new NativeArray<byte>(cellCount, Allocator.Temp);

            // Авто-расчёт целевого количества комнат из DensityFactor
            var avgRoomSide = (mapConfig.MinRoomSize + mapConfig.MaxRoomSize) / 2;
            var avgRoomArea = avgRoomSide * avgRoomSide;
            var targetRooms = math.max(1, (int)(cellCount / (float)avgRoomArea * mapConfig.DensityFactor));
            var roomAttempts = targetRooms * 3;
            var rooms = new NativeList<RoomRect>(roomAttempts, Allocator.Temp);
            // Для составных комнат: отслеживаем какие комнаты были склеены (вторая часть)
            var compositeSecondParts = new NativeList<RoomRect>(roomAttempts / 2, Allocator.Temp);

            try
            {
                // Инициализация: всё стены
                for (var i = 0; i < cellCount; i++)
                {
                    baseLayer[i] = MapCellType.Wall;
                    obstacleLayer[i] = ObstacleType.None;
                    hazardLayer[i] = HazardType.None;
                }

                // === T007: Генерация комнат с поддержкой составных форм ===
                GenerateRooms(ref rng, baseLayer, mapSize, mapConfig, roomAttempts, ref rooms, ref compositeSecondParts);

                // Если комнаты не сгенерированы — fallback
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

                // === T008: MST-коридоры + extra edges ===
                var connectedCounts = new NativeArray<int>(rooms.Length, Allocator.Temp);
                GenerateMSTCorridors(ref rng, baseLayer, mapSize, mapConfig, ref rooms, ref connectedCounts);

                var startCell = GetCenter(rooms[0]);
                EnsureSafeRadius(baseLayer, obstacleLayer, hazardLayer, mapSize, startCell, mapConfig.SafeRadius);
                SetBorderWalls(baseLayer, mapSize);
                PopulateHazards(hazardLayer, baseLayer, obstacleLayer, mapSize, startCell, mapConfig.SafeRadius, hazardConfig, runState.ValueRO.Seed);

                // === T009: Расчёт стеновых масок ===
                ComputeWallMasks(baseLayer, wallMaskLayer, mapSize);

                // === T010: Расчёт вариантов пола ===
                ComputeFloorVariants(baseLayer, floorVariantLayer, mapSize, runState.ValueRO.Seed, mapConfig.FloorVariantCount);

                rngState.ValueRW.Rng = rng;

                // Сохраняем RunId перед структурными изменениями, чтобы не обращаться к invalidated handle
                var currentRunId = runState.ValueRO.RunId;
                var currentSeed = runState.ValueRO.Seed;

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
                var wallMaskBlob = builder.Allocate(ref root.WallMaskLayer, cellCount);
                var floorVariantBlob = builder.Allocate(ref root.FloorVariantLayer, cellCount);

                for (var i = 0; i < cellCount; i++)
                {
                    baseBlob[i] = baseLayer[i];
                    obstacleBlob[i] = obstacleLayer[i];
                    hazardBlob[i] = hazardLayer[i];
                    wallMaskBlob[i] = wallMaskLayer[i];
                    floorVariantBlob[i] = floorVariantLayer[i];
                }

                // === T011: Заполняем данные комнат с типами ===
                root.RoomCount = rooms.Length;
                var roomsBlob = builder.Allocate(ref root.Rooms, rooms.Length);
                for (var i = 0; i < rooms.Length; i++)
                {
                    var r = rooms[i];
                    byte roomType;
                    if (i == 0)
                    {
                        roomType = (byte)RoomType.Start;
                    }
                    else
                    {
                        // Вероятностный выбор: Normal 60%, HardEnemy 25%, Treasure 15%
                        var roll = rng.NextFloat();
                        if (roll < 0.60f)
                            roomType = (byte)RoomType.Normal;
                        else if (roll < 0.85f)
                            roomType = (byte)RoomType.HardEnemy;
                        else
                            roomType = (byte)RoomType.Treasure;
                    }

                    roomsBlob[i] = new RoomBlob
                    {
                        Bounds = new int4(r.X, r.Y, r.Width, r.Height),
                        Center = GetCenter(r),
                        RoomType = roomType,
                        ConnectedCount = (byte)math.min(connectedCounts[i], 255)
                    };
                }

                // Сохраняем rng после использования для типов комнат
                rngState.ValueRW.Rng = rng;

                var mapBlobRef = builder.CreateBlobAssetReference<MapBlob>(Allocator.Persistent);

                // Структурные изменения — после этого runState handle невалиден
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
                    state.EntityManager.AddComponentData(mapEntity, new MapRenderRequest { RunId = currentRunId });
                }
                else
                {
                    state.EntityManager.SetComponentData(mapEntity, new MapRenderRequest { RunId = currentRunId });
                }
                state.EntityManager.SetComponentEnabled<MapRenderRequest>(mapEntity, true);

                // Добавляем FogRenderRequest и MinimapToggleTag для тумана войны и миникарты
                if (!state.EntityManager.HasComponent<FogRenderRequest>(mapEntity))
                {
                    state.EntityManager.AddComponentData(mapEntity, new FogRenderRequest());
                }
                state.EntityManager.SetComponentEnabled<FogRenderRequest>(mapEntity, false);

                if (!state.EntityManager.HasComponent<MinimapToggleTag>(mapEntity))
                {
                    state.EntityManager.AddComponentData(mapEntity, new MinimapToggleTag());
                }
                state.EntityManager.SetComponentEnabled<MinimapToggleTag>(mapEntity, false);

                var occupancy = state.EntityManager.HasComponent<CellOccupant>(mapEntity)
                    ? state.EntityManager.GetBuffer<CellOccupant>(mapEntity)
                    : state.EntityManager.AddBuffer<CellOccupant>(mapEntity);

                occupancy.Clear();
                occupancy.ResizeUninitialized(cellCount);
                for (var i = 0; i < cellCount; i++)
                {
                    occupancy[i] = new CellOccupant { Value = Entity.Null };
                }

                // Переполучаем RunState после структурных изменений
                var updatedRunState = SystemAPI.GetSingletonRW<RunState>();
                updatedRunState.ValueRW.Seed = currentSeed;
                updatedRunState.ValueRW.MapSize = mapSize;
                updatedRunState.ValueRW.StartCell = startCell;
                updatedRunState.ValueRW.SafeRadius = mapConfig.SafeRadius;
                updatedRunState.ValueRW.IsInitialized = true;


                connectedCounts.Dispose();
            }
            finally
            {
                baseLayer.Dispose();
                obstacleLayer.Dispose();
                hazardLayer.Dispose();
                wallMaskLayer.Dispose();
                floorVariantLayer.Dispose();
                rooms.Dispose();
                compositeSecondParts.Dispose();
            }
        }

        // =====================================================================
        // T007: Генерация комнат с составными формами (Г/Т-образные)
        // =====================================================================

        private static void GenerateRooms(
            ref Random rng,
            NativeArray<MapCellType> baseLayer,
            int2 mapSize,
            MapGenerationConfigData mapConfig,
            int roomAttempts,
            ref NativeList<RoomRect> rooms,
            ref NativeList<RoomRect> compositeSecondParts)
        {
            for (var i = 0; i < roomAttempts; i++)
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

                // Составная комната: с вероятностью compositeChance приклеиваем второй прямоугольник
                if (rng.NextFloat() < mapConfig.CompositeRoomChance)
                {
                    TryAddCompositeExtension(ref rng, baseLayer, mapSize, mapConfig, room, ref rooms, ref compositeSecondParts);
                }

                rooms.Add(room);
            }
        }

        /// <summary>
        /// Пытается приклеить второй прямоугольник к комнате, создавая Г/Т-образную форму.
        /// Второй прямоугольник перекрывается с основным на 1–2 клетки.
        /// </summary>
        private static void TryAddCompositeExtension(
            ref Random rng,
            NativeArray<MapCellType> baseLayer,
            int2 mapSize,
            MapGenerationConfigData mapConfig,
            RoomRect mainRoom,
            ref NativeList<RoomRect> rooms,
            ref NativeList<RoomRect> compositeSecondParts)
        {
            var extWidth = rng.NextInt(mapConfig.MinRoomSize, math.max(mapConfig.MinRoomSize + 1, mainRoom.Width));
            var extHeight = rng.NextInt(mapConfig.MinRoomSize, math.max(mapConfig.MinRoomSize + 1, mainRoom.Height));

            // Выбираем сторону приклейки (0=вправо, 1=вниз, 2=влево, 3=вверх)
            var side = rng.NextInt(0, 4);
            int extX, extY;
            switch (side)
            {
                case 0: // Вправо
                    extX = mainRoom.XMax - rng.NextInt(1, math.min(3, extWidth));
                    extY = mainRoom.Y + rng.NextInt(0, math.max(1, mainRoom.Height - extHeight + 1));
                    break;
                case 1: // Вверх
                    extX = mainRoom.X + rng.NextInt(0, math.max(1, mainRoom.Width - extWidth + 1));
                    extY = mainRoom.YMax - rng.NextInt(1, math.min(3, extHeight));
                    break;
                case 2: // Влево
                    extX = mainRoom.X - extWidth + rng.NextInt(1, math.min(3, extWidth));
                    extY = mainRoom.Y + rng.NextInt(0, math.max(1, mainRoom.Height - extHeight + 1));
                    break;
                default: // Вниз
                    extX = mainRoom.X + rng.NextInt(0, math.max(1, mainRoom.Width - extWidth + 1));
                    extY = mainRoom.Y - extHeight + rng.NextInt(1, math.min(3, extHeight));
                    break;
            }

            var ext = new RoomRect { X = extX, Y = extY, Width = extWidth, Height = extHeight };

            // Проверяем, что расширение в границах карты
            if (ext.XMin < 1 || ext.YMin < 1 || ext.XMax >= mapSize.x - 1 || ext.YMax >= mapSize.y - 1)
            {
                return;
            }

            // Проверяем перекрытие с другими комнатами (исключая основную)
            if (IsOverlapping(ext, ref rooms))
            {
                return;
            }

            CarveRoom(baseLayer, mapSize, ext);
            compositeSecondParts.Add(ext);
        }

        // =====================================================================
        // T008: MST-коридоры + extra edges с вариативной шириной
        // =====================================================================

        private static void GenerateMSTCorridors(
            ref Random rng,
            NativeArray<MapCellType> baseLayer,
            int2 mapSize,
            MapGenerationConfigData mapConfig,
            ref NativeList<RoomRect> rooms,
            ref NativeArray<int> connectedCounts)
        {
            if (rooms.Length <= 1)
            {
                return;
            }

            var roomCount = rooms.Length;

            // Построить полный граф рёбер между всеми парами комнат
            var edgeCount = roomCount * (roomCount - 1) / 2;
            var edges = new NativeList<RoomEdge>(edgeCount, Allocator.Temp);

            for (var i = 0; i < roomCount; i++)
            {
                var ci = GetCenter(rooms[i]);
                for (var j = i + 1; j < roomCount; j++)
                {
                    var cj = GetCenter(rooms[j]);
                    var dx = (float)(ci.x - cj.x);
                    var dy = (float)(ci.y - cj.y);
                    edges.Add(new RoomEdge
                    {
                        RoomA = i,
                        RoomB = j,
                        Distance = dx * dx + dy * dy // Квадрат расстояния (сортировка по нему эквивалентна)
                    });
                }
            }

            // Сортировка рёбер по расстоянию (insertion sort — достаточно для ~100 рёбер)
            SortEdges(ref edges);

            // MST через Kruskal (Union-Find)
            var parent = new NativeArray<int>(roomCount, Allocator.Temp);
            var rank = new NativeArray<int>(roomCount, Allocator.Temp);
            for (var i = 0; i < roomCount; i++)
            {
                parent[i] = i;
                rank[i] = 0;
            }

            var mstEdges = new NativeList<RoomEdge>(roomCount - 1, Allocator.Temp);
            var nonMstEdges = new NativeList<RoomEdge>(edges.Length, Allocator.Temp);

            for (var i = 0; i < edges.Length; i++)
            {
                var edge = edges[i];
                var rootA = Find(ref parent, edge.RoomA);
                var rootB = Find(ref parent, edge.RoomB);

                if (rootA != rootB)
                {
                    mstEdges.Add(edge);
                    Union(ref parent, ref rank, rootA, rootB);
                }
                else
                {
                    nonMstEdges.Add(edge);
                }
            }

            // Прорисовываем MST-коридоры
            for (var i = 0; i < mstEdges.Length; i++)
            {
                var edge = mstEdges[i];
                var from = GetCenter(rooms[edge.RoomA]);
                var to = GetCenter(rooms[edge.RoomB]);
                var corridorWidth = rng.NextInt(mapConfig.MinCorridorWidth, mapConfig.MaxCorridorWidth + 1);
                CarveWideCorridor(baseLayer, mapSize, from, to, corridorWidth, ref rng);
                connectedCounts[edge.RoomA]++;
                connectedCounts[edge.RoomB]++;
            }

            // Дополнительные рёбра (extraCorridorRatio) — создают петли и развилки
            var extraCount = math.max(0, (int)(nonMstEdges.Length * mapConfig.ExtraCorridorRatio));
            for (var i = 0; i < extraCount && i < nonMstEdges.Length; i++)
            {
                // Берём случайное ребро из оставшихся
                var idx = rng.NextInt(0, nonMstEdges.Length);
                var edge = nonMstEdges[idx];
                var from = GetCenter(rooms[edge.RoomA]);
                var to = GetCenter(rooms[edge.RoomB]);
                var corridorWidth = rng.NextInt(mapConfig.MinCorridorWidth, mapConfig.MaxCorridorWidth + 1);
                CarveWideCorridor(baseLayer, mapSize, from, to, corridorWidth, ref rng);
                connectedCounts[edge.RoomA]++;
                connectedCounts[edge.RoomB]++;

                // Удаляем использованное ребро (swap с последним)
                nonMstEdges[idx] = nonMstEdges[nonMstEdges.Length - 1];
                nonMstEdges.RemoveAt(nonMstEdges.Length - 1);
            }

            edges.Dispose();
            parent.Dispose();
            rank.Dispose();
            mstEdges.Dispose();
            nonMstEdges.Dispose();
        }

        /// <summary>
        /// Прокладывает L-образный коридор заданной ширины.
        /// </summary>
        private static void CarveWideCorridor(NativeArray<MapCellType> baseLayer, int2 size, int2 from, int2 to, int width, ref Random rng)
        {
            // Половина ширины для расширения в обе стороны
            var halfWidth = width / 2;

            if (rng.NextFloat() < 0.5f)
            {
                // Горизонтальный сегмент, затем вертикальный
                CarveHorizontalWide(baseLayer, size, from.x, to.x, from.y, halfWidth);
                CarveVerticalWide(baseLayer, size, from.y, to.y, to.x, halfWidth);
            }
            else
            {
                // Вертикальный сегмент, затем горизонтальный
                CarveVerticalWide(baseLayer, size, from.y, to.y, from.x, halfWidth);
                CarveHorizontalWide(baseLayer, size, from.x, to.x, to.y, halfWidth);
            }
        }

        private static void CarveHorizontalWide(NativeArray<MapCellType> baseLayer, int2 size, int x0, int x1, int y, int halfWidth)
        {
            var startX = math.min(x0, x1);
            var endX = math.max(x0, x1);
            for (var x = startX; x <= endX; x++)
            {
                for (var dy = -halfWidth; dy <= halfWidth; dy++)
                {
                    var cy = y + dy;
                    if (cy >= 1 && cy < size.y - 1 && x >= 0 && x < size.x)
                    {
                        baseLayer[cy * size.x + x] = MapCellType.Floor;
                    }
                }
            }
        }

        private static void CarveVerticalWide(NativeArray<MapCellType> baseLayer, int2 size, int y0, int y1, int x, int halfWidth)
        {
            var startY = math.min(y0, y1);
            var endY = math.max(y0, y1);
            for (var y = startY; y <= endY; y++)
            {
                for (var dx = -halfWidth; dx <= halfWidth; dx++)
                {
                    var cx = x + dx;
                    if (cx >= 1 && cx < size.x - 1 && y >= 0 && y < size.y)
                    {
                        baseLayer[y * size.x + cx] = MapCellType.Floor;
                    }
                }
            }
        }

        // =====================================================================
        // T009: Расчёт 4-bit стеновых масок (N=1, E=2, S=4, W=8)
        // =====================================================================

        private static void ComputeWallMasks(NativeArray<MapCellType> baseLayer, NativeArray<byte> wallMaskLayer, int2 size)
        {
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var index = y * size.x + x;
                    if (baseLayer[index] != MapCellType.Wall)
                    {
                        wallMaskLayer[index] = 0;
                        continue;
                    }

                    byte mask = 0;

                    // N (y+1) — сосед сверху
                    if (y + 1 < size.y && baseLayer[(y + 1) * size.x + x] == MapCellType.Wall)
                        mask |= 1;
                    // E (x+1) — сосед справа
                    if (x + 1 < size.x && baseLayer[y * size.x + (x + 1)] == MapCellType.Wall)
                        mask |= 2;
                    // S (y-1) — сосед снизу
                    if (y - 1 >= 0 && baseLayer[(y - 1) * size.x + x] == MapCellType.Wall)
                        mask |= 4;
                    // W (x-1) — сосед слева
                    if (x - 1 >= 0 && baseLayer[y * size.x + (x - 1)] == MapCellType.Wall)
                        mask |= 8;

                    wallMaskLayer[index] = mask;
                }
            }
        }

        // =====================================================================
        // T010: Детерминированные варианты пола через xxHash
        // =====================================================================

        private static void ComputeFloorVariants(NativeArray<MapCellType> baseLayer, NativeArray<byte> floorVariantLayer, int2 size, uint seed, int variantCount)
        {
            if (variantCount <= 1)
            {
                return; // Все нули — один вариант
            }

            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var index = y * size.x + x;
                    if (baseLayer[index] != MapCellType.Floor)
                    {
                        continue;
                    }

                    // xxHash: детерминированный хеш от (seed, cellIndex)
                    var hash = math.hash(new uint2(seed, (uint)index));
                    floorVariantLayer[index] = (byte)(hash % (uint)variantCount);
                }
            }
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        private static void CarveRoom(NativeArray<MapCellType> baseLayer, int2 size, RoomRect room)
        {
            for (var y = room.YMin; y < room.YMax; y++)
            {
                for (var x = room.XMin; x < room.XMax; x++)
                {
                    if (x >= 0 && x < size.x && y >= 0 && y < size.y)
                    {
                        baseLayer[y * size.x + x] = MapCellType.Floor;
                    }
                }
            }
        }

        private static int2 GetCenter(RoomRect room)
        {
            return new int2(room.X + room.Width / 2, room.Y + room.Height / 2);
        }

        /// <summary>
        /// Проверяет перекрытие комнаты с существующими, с зазором minGap клеток.
        /// Зазор 2+ необходим чтобы коридоры шириной 3 не разрушали стены между комнатами.
        /// </summary>
        private static bool IsOverlapping(RoomRect room, ref NativeList<RoomRect> rooms, int minGap = 2)
        {
            var expanded = new RoomRect
            {
                X = room.X - minGap,
                Y = room.Y - minGap,
                Width = room.Width + minGap * 2,
                Height = room.Height + minGap * 2
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

        // =====================================================================
        // Union-Find для Kruskal MST
        // =====================================================================

        private static int Find(ref NativeArray<int> parent, int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]]; // Path compression
                x = parent[x];
            }
            return x;
        }

        private static void Union(ref NativeArray<int> parent, ref NativeArray<int> rank, int a, int b)
        {
            if (rank[a] < rank[b])
            {
                parent[a] = b;
            }
            else if (rank[a] > rank[b])
            {
                parent[b] = a;
            }
            else
            {
                parent[b] = a;
                rank[a]++;
            }
        }

        /// <summary>
        /// Сортировка рёбер по расстоянию (insertion sort — для ~100 рёбер достаточно).
        /// </summary>
        private static void SortEdges(ref NativeList<RoomEdge> edges)
        {
            for (var i = 1; i < edges.Length; i++)
            {
                var key = edges[i];
                var j = i - 1;
                while (j >= 0 && edges[j].Distance > key.Distance)
                {
                    edges[j + 1] = edges[j];
                    j--;
                }
                edges[j + 1] = key;
            }
        }
    }
}
