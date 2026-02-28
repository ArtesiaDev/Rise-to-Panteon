using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerAttackSystem))]
    public partial struct EnemyAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RunState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (position, target, cooldown, range, damage) 
                     in SystemAPI.Query<RefRO<GridPosition>, RefRW<Target>, RefRW<AttackCooldown>, RefRO<AttackRange>, RefRO<Damage>>().WithAll<EnemyTag>())
            {
                if (!target.ValueRO.HasTarget || !state.EntityManager.Exists(target.ValueRO.Value) || !SystemAPI.HasComponent<Health>(target.ValueRO.Value))
                {
                    target.ValueRW.HasTarget = false;
                    continue;
                }

                if (cooldown.ValueRO.Remaining > 0f)
                {
                    continue;
                }

                var attackerPos = position.ValueRO.Value;
                var targetPos = SystemAPI.GetComponent<GridPosition>(target.ValueRO.Value).Value;
                var distance = math.abs(attackerPos.x - targetPos.x) + math.abs(attackerPos.y - targetPos.y);
                if (distance > range.ValueRO.Value)
                {
                    continue;
                }

                var health = SystemAPI.GetComponent<Health>(target.ValueRO.Value);
                health.Current = math.max(0, health.Current - damage.ValueRO.Value);
                SystemAPI.SetComponent(target.ValueRO.Value, health);

                cooldown.ValueRW.Remaining = cooldown.ValueRO.Interval;
            }
        }
    }
}
