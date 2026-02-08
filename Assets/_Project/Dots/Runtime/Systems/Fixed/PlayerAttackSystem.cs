using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MovementResolveSystem))]
    public partial struct PlayerAttackSystem : ISystem
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

            foreach (var (attackRequest, cooldown, damage, direction, position, range, stats, entity) in SystemAPI.Query<RefRO<AttackRequest>, RefRW<AttackCooldown>, RefRO<Damage>, RefRO<LastMoveDirection>, RefRO<GridPosition>, RefRO<AttackRange>, RefRO<PlayerStats>>().WithAll<PlayerTag>().WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<AttackRequest>(entity))
                {
                    continue;
                }

                if (cooldown.ValueRO.Remaining > 0f)
                {
                    SystemAPI.SetComponentEnabled<AttackRequest>(entity, false);
                    continue;
                }

                var origin = position.ValueRO.Value;
                var dir = direction.ValueRO.Value;
                var steps = math.max(1, (int)math.round(range.ValueRO.Value));
                var totalDamage = damage.ValueRO.Value + stats.ValueRO.BonusDamage;

                for (var step = 1; step <= steps; step++)
                {
                    var targetCell = origin + dir * step;
                    if (!MapUtilities.InBounds(targetCell, map.Size))
                    {
                        break;
                    }

                    var index = MapUtilities.ToIndex(targetCell, map.Size);
                    var targetEntity = occupancy[index].Value;
                    if (targetEntity == Entity.Null)
                    {
                        continue;
                    }

                    if (SystemAPI.HasComponent<EnemyTag>(targetEntity) && SystemAPI.HasComponent<Health>(targetEntity))
                    {
                        var health = SystemAPI.GetComponent<Health>(targetEntity);
                        health.Current = (int)math.max(0, health.Current - totalDamage);
                        SystemAPI.SetComponent(targetEntity, health);
                    }
                }

                cooldown.ValueRW.Remaining = cooldown.ValueRO.Interval;
                SystemAPI.SetComponentEnabled<AttackRequest>(entity, false);
            }
        }
    }
}
