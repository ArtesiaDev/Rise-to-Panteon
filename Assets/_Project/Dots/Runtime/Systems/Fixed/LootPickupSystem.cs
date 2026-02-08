using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PoisonTickSystem))]
    public partial struct LootPickupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var hasPlayer = false;
            Entity playerEntity = Entity.Null;
            int2 playerCell = int2.zero;

            foreach (var (position, entity) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<PlayerTag>().WithEntityAccess())
            {
                playerEntity = entity;
                playerCell = position.ValueRO.Value;
                hasPlayer = true;
                break;
            }

            if (!hasPlayer)
            {
                return;
            }

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (position, loot, entity) in SystemAPI.Query<RefRO<GridPosition>, RefRO<LootPickup>>().WithAll<LootTag>().WithEntityAccess())
            {
                if (!position.ValueRO.Value.Equals(playerCell))
                {
                    continue;
                }

                if (SystemAPI.HasComponent<PlayerStats>(playerEntity))
                {
                    var stats = SystemAPI.GetComponent<PlayerStats>(playerEntity);
                    if (loot.ValueRO.Type == PickupType.Gold)
                    {
                        stats.Gold += loot.ValueRO.Amount;
                    }
                    else if (loot.ValueRO.Type == PickupType.Xp)
                    {
                        stats.Xp += loot.ValueRO.Amount;
                    }

                    SystemAPI.SetComponent(playerEntity, stats);
                }

                if (loot.ValueRO.Type == PickupType.Heal && SystemAPI.HasComponent<Health>(playerEntity))
                {
                    var health = SystemAPI.GetComponent<Health>(playerEntity);
                    health.Current = math.min(health.Max, health.Current + loot.ValueRO.Amount);
                    SystemAPI.SetComponent(playerEntity, health);
                }

                ecb.DestroyEntity(entity);
            }
        }
    }
}
