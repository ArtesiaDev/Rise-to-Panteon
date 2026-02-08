using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyPathfindSystem))]
    public partial struct EnemyMoveIntentSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var rngState = SystemAPI.GetSingletonRW<RngState>();
            var rng = rngState.ValueRW.Rng;

            foreach (var (position, target, path, pathIndex, idle, entity) in SystemAPI.Query<RefRO<GridPosition>, RefRO<Target>, DynamicBuffer<PathStep>, RefRO<PathIndex>, RefRW<IdleMoveCooldown>>().WithAll<EnemyTag>().WithEntityAccess())
            {
                var hasPathStep = target.ValueRO.HasTarget && pathIndex.ValueRO.Value < path.Length;
                if (hasPathStep)
                {
                    var current = position.ValueRO.Value;
                    var next = path[pathIndex.ValueRO.Value].Value;
                    var direction = new int2(next.x - current.x, next.y - current.y);
                    SystemAPI.SetComponent(entity, new MoveIntent { Direction = direction });
                    SystemAPI.SetComponentEnabled<MoveIntent>(entity, true);
                    continue;
                }

                if (idle.ValueRO.Remaining > 0f)
                {
                    SystemAPI.SetComponentEnabled<MoveIntent>(entity, false);
                    continue;
                }

                var roll = rng.NextInt(0, 4);
                var direction = roll switch
                {
                    0 => new int2(0, 1),
                    1 => new int2(1, 0),
                    2 => new int2(0, -1),
                    _ => new int2(-1, 0)
                };

                SystemAPI.SetComponent(entity, new MoveIntent { Direction = direction });
                SystemAPI.SetComponentEnabled<MoveIntent>(entity, true);
                idle.ValueRW.Remaining = idle.ValueRO.Interval;
            }

            rngState.ValueRW.Rng = rng;
        }
    }
}
