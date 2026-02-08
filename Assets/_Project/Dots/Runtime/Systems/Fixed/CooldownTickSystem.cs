using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(DifficultyTickSystem))]
    public partial struct CooldownTickSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var cooldown in SystemAPI.Query<RefRW<MoveCooldown>>())
            {
                cooldown.ValueRW.Remaining = math.max(0f, cooldown.ValueRO.Remaining - deltaTime);
            }

            foreach (var cooldown in SystemAPI.Query<RefRW<AttackCooldown>>())
            {
                cooldown.ValueRW.Remaining = math.max(0f, cooldown.ValueRO.Remaining - deltaTime);
            }

            foreach (var cooldown in SystemAPI.Query<RefRW<PathRefreshCooldown>>())
            {
                cooldown.ValueRW.Remaining = math.max(0f, cooldown.ValueRO.Remaining - deltaTime);
            }

            foreach (var cooldown in SystemAPI.Query<RefRW<IdleMoveCooldown>>())
            {
                cooldown.ValueRW.Remaining = math.max(0f, cooldown.ValueRO.Remaining - deltaTime);
            }
        }
    }
}
