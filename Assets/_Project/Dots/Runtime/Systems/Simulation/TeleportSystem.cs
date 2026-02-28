using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    /// <summary>
    /// DevTool: телепортирует игрока на указанную свободную клетку по запросу из InputState.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InputReadSystem))]
    public partial struct TeleportSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputState>();
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<MapBlobReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingletonRW<InputState>();
            if (!input.ValueRO.TeleportRequested)
            {
                return;
            }

            var target = input.ValueRO.TeleportTarget;
            input.ValueRW.TeleportRequested = false;

            var mapRef = SystemAPI.GetSingleton<MapBlobReference>();
            if (!mapRef.Value.IsCreated)
            {
                return;
            }

            ref var map = ref mapRef.Value.Value;

            // Проверяем что целевая клетка валидна
            if (!MapUtilities.InBounds(target, map.Size) || !MapUtilities.IsWalkable(ref map, target))
            {
                return;
            }

            var runEntity = SystemAPI.GetSingletonEntity<RunState>();
            var occupancy = state.EntityManager.GetBuffer<CellOccupant>(runEntity);
            var targetIndex = MapUtilities.ToIndex(target, map.Size);

            // Проверяем что целевая клетка свободна
            if (occupancy[targetIndex].Value != Entity.Null)
            {
                return;
            }

            // Находим player entity
            foreach (var (position, _, entity) in SystemAPI
                         .Query<RefRW<GridPosition>, RefRO<PlayerTag>>()
                         .WithEntityAccess())
            {
                var oldPos = position.ValueRO.Value;
                var oldIndex = MapUtilities.ToIndex(oldPos, map.Size);

                // Обновляем occupancy
                occupancy[oldIndex] = new CellOccupant { Value = Entity.Null };
                occupancy[targetIndex] = new CellOccupant { Value = entity };

                // Обновляем позицию
                position.ValueRW.Value = target;
                SystemAPI.SetComponent(entity, new PreviousGridPosition { Value = target });
                SystemAPI.SetComponent(entity, new RenderPosition { Value = new float2(target.x, target.y) });

                break;
            }
        }
    }
}
