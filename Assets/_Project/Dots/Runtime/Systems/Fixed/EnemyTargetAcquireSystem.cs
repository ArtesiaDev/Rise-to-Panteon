using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(EnemySpawnerSystem))]
    public partial struct EnemyTargetAcquireSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RunState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var hasPlayer = false;
            Entity playerEntity = Entity.Null;
            int2 playerCell = int2.zero;

            foreach (var (position, entity) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<PlayerTag>().WithEntityAccess())
            {
                playerCell = position.ValueRO.Value;
                playerEntity = entity;
                hasPlayer = true;
                break;
            }

            foreach (var (position, aggro, target) in SystemAPI.Query<RefRO<GridPosition>, RefRO<AggroRange>, RefRW<Target>>().WithAll<EnemyTag>())
            {
                if (!hasPlayer)
                {
                    target.ValueRW.HasTarget = false;
                    continue;
                }

                var enemyCell = position.ValueRO.Value;
                var distance = math.abs(enemyCell.x - playerCell.x) + math.abs(enemyCell.y - playerCell.y);
                if (distance <= aggro.ValueRO.Value)
                {
                    target.ValueRW.Value = playerEntity;
                    target.ValueRW.HasTarget = true;
                }
                else
                {
                    target.ValueRW.HasTarget = false;
                }
            }
        }
    }
}
