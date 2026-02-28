using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMoveIntentSystem))]
    public partial struct MovementResolveSystem : ISystem
    {
        private struct MoveRequest
        {
            public Entity Entity;
            public int2 From;
            public int2 To;
            public bool IsPlayer;
            public int EntityIndex;
        }

        private struct MoveRequestComparer : IComparer<MoveRequest>
        {
            public int Compare(MoveRequest a, MoveRequest b)
            {
                if (a.IsPlayer != b.IsPlayer)
                {
                    return a.IsPlayer ? -1 : 1;
                }

                return a.EntityIndex.CompareTo(b.EntityIndex);
            }
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<MapBlobReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var mapRef = SystemAPI.GetSingleton<MapBlobReference>();
            if (!mapRef.Value.IsCreated)
            {
                return;
            }

            ref var map = ref mapRef.Value.Value;
            var occupancy = state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>());

            using var requests = new NativeList<MoveRequest>(Allocator.Temp);

            foreach (var (position, intent, _, cooldown, entity) 
                     in SystemAPI.Query<RefRO<GridPosition>, RefRO<MoveIntent>, RefRO<MoveSpeed>, RefRW<MoveCooldown>>().WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<MoveIntent>(entity))
                {
                    continue;
                }

                if (cooldown.ValueRO.Remaining > 0f || intent.ValueRO.Direction.Equals(int2.zero))
                {
                    SystemAPI.SetComponentEnabled<MoveIntent>(entity, false);
                    continue;
                }

                var from = position.ValueRO.Value;
                var to = from + intent.ValueRO.Direction;
                if (!MapUtilities.IsWalkable(ref map, to))
                {
                    SystemAPI.SetComponentEnabled<MoveIntent>(entity, false);
                    continue;
                }

                requests.Add(new MoveRequest
                {
                    Entity = entity,
                    From = from,
                    To = to,
                    IsPlayer = SystemAPI.HasComponent<PlayerTag>(entity),
                    EntityIndex = entity.Index
                });
            }

            requests.AsArray().Sort(new MoveRequestComparer());

            foreach (var request in requests)
            {
                var toIndex = MapUtilities.ToIndex(request.To, map.Size);
                if (occupancy[toIndex].Value != Entity.Null)
                {
                    continue;
                }

                var fromIndex = MapUtilities.ToIndex(request.From, map.Size);
                occupancy[fromIndex] = new CellOccupant { Value = Entity.Null };
                occupancy[toIndex] = new CellOccupant { Value = request.Entity };

                // PreviousGridPosition уже добавляется при спавне всех entity
                SystemAPI.SetComponent(request.Entity, new PreviousGridPosition { Value = request.From });

                SystemAPI.SetComponent(request.Entity, new GridPosition { Value = request.To });

                if (SystemAPI.HasComponent<LastMoveDirection>(request.Entity))
                {
                    SystemAPI.SetComponent(request.Entity, new LastMoveDirection { Value = request.To - request.From });
                }

                if (SystemAPI.HasComponent<MoveCooldown>(request.Entity))
                {
                    var speed = SystemAPI.GetComponent<MoveSpeed>(request.Entity).CellsPerSecond;
                    if (SystemAPI.HasComponent<PlayerStats>(request.Entity))
                    {
                        speed *= SystemAPI.GetComponent<PlayerStats>(request.Entity).MoveSpeedMult;
                    }

                    SystemAPI.SetComponent(request.Entity, new MoveCooldown { Remaining = speed > 0.01f ? 1f / speed : 0.1f });
                }

                if (SystemAPI.HasComponent<PathIndex>(request.Entity) && SystemAPI.HasBuffer<PathStep>(request.Entity))
                {
                    var pathIndex = SystemAPI.GetComponent<PathIndex>(request.Entity);
                    var buffer = SystemAPI.GetBuffer<PathStep>(request.Entity);
                    if (pathIndex.Value < buffer.Length && buffer[pathIndex.Value].Value.Equals(request.To))
                    {
                        pathIndex.Value += 1;
                        SystemAPI.SetComponent(request.Entity, pathIndex);
                    }
                }

                SystemAPI.SetComponentEnabled<MoveIntent>(request.Entity, false);
            }
        }
    }
}
