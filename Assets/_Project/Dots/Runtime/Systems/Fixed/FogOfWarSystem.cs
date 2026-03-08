using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    /// <summary>
    /// Система тумана войны: shadowcasting от позиции игрока.
    /// Persistent NativeArray<FogState> — аналог A*-массивов в EnemyPathfindSystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MovementResolveSystem))]
    public partial struct FogOfWarSystem : ISystem
    {
        // Persistent массив видимости — переиспользуется между кадрами
        private NativeArray<FogState> _fogState;
        private int _allocatedSize;
        private int2 _lastPlayerPos;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<MapBlobReference>();
            state.RequireForUpdate<VisibilityConfigData>();
            _allocatedSize = 0;
            _lastPlayerPos = new int2(-1, -1);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_allocatedSize > 0)
            {
                _fogState.Dispose();
                _allocatedSize = 0;
            }
        }

        // [BurstCompile] снят с OnUpdate — используется EntityManager для FogRenderRequest toggle.
        // CastLight остаётся Burst-совместимым (чистая арифметика).
        public void OnUpdate(ref SystemState state)
        {
            var visConfig = SystemAPI.GetSingleton<VisibilityConfigData>();
            if (!visConfig.Enabled)
            {
                return;
            }

            var runState = SystemAPI.GetSingleton<RunState>();
            if (!runState.IsInitialized)
            {
                return;
            }

            var mapRef = SystemAPI.GetSingleton<MapBlobReference>();
            if (!mapRef.Value.IsCreated)
            {
                return;
            }

            ref var map = ref mapRef.Value.Value;
            var total = map.Size.x * map.Size.y;

            // Пересоздаём массив если размер карты изменился (рестарт)
            if (total != _allocatedSize)
            {
                if (_allocatedSize > 0)
                {
                    _fogState.Dispose();
                }

                _fogState = new NativeArray<FogState>(total, Allocator.Persistent);
                _allocatedSize = total;
                _lastPlayerPos = new int2(-1, -1);
            }

            // Находим позицию игрока
            var playerPos = int2.zero;
            var foundPlayer = false;
            foreach (var pos in SystemAPI.Query<RefRO<GridPosition>>().WithAll<PlayerTag>())
            {
                playerPos = pos.ValueRO.Value;
                foundPlayer = true;
                break;
            }

            if (!foundPlayer)
            {
                return;
            }

            // Пропускаем пересчёт если игрок не двигался
            if (math.all(_lastPlayerPos == playerPos))
            {
                return;
            }

            _lastPlayerPos = playerPos;

            // Шаг 1: все Visible → Explored
            for (var i = 0; i < total; i++)
            {
                if (_fogState[i] == FogState.Visible)
                {
                    _fogState[i] = FogState.Explored;
                }
            }

            // Шаг 2: Symmetric shadowcasting — помечаем видимые клетки
            var radius = visConfig.ViewRadius;

            // Клетка игрока всегда видна
            var playerIndex = playerPos.y * map.Size.x + playerPos.x;
            if (playerIndex >= 0 && playerIndex < total)
            {
                _fogState[playerIndex] = FogState.Visible;
            }

            // Обрабатываем 8 октантов
            for (var octant = 0; octant < 8; octant++)
            {
                CastLight(ref map, ref _fogState, playerPos, radius, 1, 1.0f, 0.0f, octant);
            }

            // Ставим FogRenderRequest
            var mapEntity = SystemAPI.GetSingletonEntity<RunState>();
            if (state.EntityManager.HasComponent<FogRenderRequest>(mapEntity))
            {
                state.EntityManager.SetComponentEnabled<FogRenderRequest>(mapEntity, true);
            }
        }

        /// <summary>
        /// Возвращает NativeArray<FogState> для чтения из Hybrid-мостов.
        /// Вызывается из FogRenderBridge и MinimapBridge.
        /// </summary>
        public NativeArray<FogState> GetFogState() => _fogState;
        public bool IsAllocated => _allocatedSize > 0;

        // =====================================================================
        // Recursive Shadowcasting (октантный алгоритм)
        // =====================================================================

        /// <summary>
        /// Рекурсивный shadowcasting для одного октанта.
        /// Глубина рекурсии ограничена radius (обычно 8-12) — безопасно для стека.
        /// </summary>
        private static void CastLight(
            ref MapBlob map,
            ref NativeArray<FogState> fogState,
            int2 origin,
            int radius,
            int row,
            float startSlope,
            float endSlope,
            int octant)
        {
            if (startSlope < endSlope)
                return;

            if (row > radius)
                return;

            var newStart = startSlope;

            for (var j = row; j <= radius; j++)
            {
                var blocked = false;

                for (var dx = -j; dx <= 0; dx++)
                {
                    var dy = -j;
                    var cell = TransformOctant(origin, dx, dy, octant);

                    if (!MapUtilities.InBounds(cell, map.Size))
                        continue;

                    var leftSlope = (dx - 0.5f) / (dy + 0.5f);
                    var rightSlope = (dx + 0.5f) / (dy - 0.5f);

                    if (startSlope < rightSlope)
                        continue;
                    if (endSlope > leftSlope)
                        break;

                    // Проверяем расстояние (круглый радиус)
                    var dist2 = dx * dx + dy * dy;
                    if (dist2 > radius * radius)
                        continue;

                    var index = cell.y * map.Size.x + cell.x;
                    var isWall = map.BaseLayer[index] == MapCellType.Wall;

                    // Помечаем клетку видимой
                    fogState[index] = FogState.Visible;

                    if (blocked)
                    {
                        if (isWall)
                        {
                            newStart = rightSlope;
                        }
                        else
                        {
                            blocked = false;
                            startSlope = newStart;
                        }
                    }
                    else if (isWall && j < radius)
                    {
                        blocked = true;
                        // Рекурсия: обрабатываем сектор до стены
                        CastLight(ref map, ref fogState, origin, radius,
                            j + 1, startSlope, leftSlope, octant);
                        newStart = rightSlope;
                    }
                }

                if (blocked)
                    return;
            }
        }

        /// <summary>
        /// Преобразует координаты (dx, dy) в реальные по октанту.
        /// </summary>
        private static int2 TransformOctant(int2 origin, int dx, int dy, int octant)
        {
            return octant switch
            {
                0 => new int2(origin.x + dx, origin.y + dy),
                1 => new int2(origin.x + dy, origin.y + dx),
                2 => new int2(origin.x - dy, origin.y + dx),
                3 => new int2(origin.x - dx, origin.y + dy),
                4 => new int2(origin.x - dx, origin.y - dy),
                5 => new int2(origin.x - dy, origin.y - dx),
                6 => new int2(origin.x + dy, origin.y - dx),
                _ => new int2(origin.x + dx, origin.y - dy),
            };
        }
    }
}
