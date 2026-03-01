using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(HazardSystem))]
    public partial struct PoisonTickSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (poison, health, entity) in SystemAPI.Query<RefRW<PoisonEffect>, RefRW<Health>>().WithEntityAccess())
            {
                poison.ValueRW.Remaining -= deltaTime;
                poison.ValueRW.NextTick -= deltaTime;

                if (poison.ValueRW.NextTick <= 0f)
                {
                    poison.ValueRW.NextTick = poison.ValueRW.TickInterval;
                    var damage = math.max(1, (int)math.round(poison.ValueRO.Dps * poison.ValueRO.TickInterval));
                    health.ValueRW.Current = math.max(0, health.ValueRO.Current - damage);
                }

                if (poison.ValueRW.Remaining <= 0f)
                {
                    ecb.RemoveComponent<PoisonEffect>(entity);
                }
            }
        }
    }
}
