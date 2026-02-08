using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyAttackSystem))]
    public partial struct HazardSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MapBlobReference>();
            state.RequireForUpdate<HazardConfigData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var mapRef = SystemAPI.GetSingleton<MapBlobReference>();
            if (!mapRef.Value.IsCreated)
            {
                return;
            }

            ref var map = ref mapRef.Value.Value;
            var config = SystemAPI.GetSingleton<HazardConfigData>();
            var deltaTime = SystemAPI.Time.DeltaTime;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (position, health, hazardState, entity) in SystemAPI.Query<RefRO<GridPosition>, RefRW<Health>, RefRW<HazardState>>().WithEntityAccess())
            {
                var cell = position.ValueRO.Value;
                if (!MapUtilities.InBounds(cell, map.Size))
                {
                    continue;
                }

                var index = MapUtilities.ToIndex(cell, map.Size);
                var hazard = map.HazardLayer[index];

                if (hazardState.ValueRO.Current != hazard)
                {
                    hazardState.ValueRW.Current = hazard;
                    hazardState.ValueRW.SpikeTickRemaining = 0f;

                    if (hazard == HazardType.Spike)
                    {
                        health.ValueRW.Current = math.max(0, health.ValueRO.Current - config.SpikeDamage);
                        hazardState.ValueRW.SpikeTickRemaining = config.SpikeTickInterval;
                    }
                    else if (hazard == HazardType.Poison)
                    {
                        var poison = new PoisonEffect
                        {
                            Remaining = config.PoisonDuration,
                            Dps = config.PoisonDps,
                            TickInterval = math.clamp(config.PoisonTickInterval, 0.05f, 10f),
                            NextTick = math.clamp(config.PoisonTickInterval, 0.05f, 10f)
                        };

                        if (SystemAPI.HasComponent<PoisonEffect>(entity))
                        {
                            var existing = SystemAPI.GetComponent<PoisonEffect>(entity);
                            poison.Remaining = math.max(existing.Remaining, poison.Remaining);
                            poison.Dps = math.max(existing.Dps, poison.Dps);
                            poison.TickInterval = math.max(existing.TickInterval, poison.TickInterval);
                            poison.NextTick = math.max(existing.NextTick, poison.NextTick);
                            SystemAPI.SetComponent(entity, poison);
                        }
                        else
                        {
                            ecb.AddComponent(entity, poison);
                        }
                    }
                }

                if (hazardState.ValueRO.Current != HazardType.Spike)
                {
                    continue;
                }

                hazardState.ValueRW.SpikeTickRemaining -= deltaTime;
                if (hazardState.ValueRW.SpikeTickRemaining <= 0f)
                {
                    hazardState.ValueRW.SpikeTickRemaining = config.SpikeTickInterval;
                    health.ValueRW.Current = math.max(0, health.ValueRO.Current - config.SpikeDamage);
                }
            }
        }
    }
}
