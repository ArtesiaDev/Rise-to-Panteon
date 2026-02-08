using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CooldownTickSystem))]
    [UpdateBefore(typeof(EnemySpawnerSystem))]
    public partial struct DifficultyTickSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DifficultyState>();
            state.RequireForUpdate<DifficultyConfigData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<DifficultyConfigData>();
            var difficulty = SystemAPI.GetSingletonRW<DifficultyState>();
            difficulty.ValueRW.ElapsedTime += SystemAPI.Time.DeltaTime;

            var timeFactor = math.saturate(difficulty.ValueRO.ElapsedTime / math.max(1f, config.TimeToMaxDifficulty));
            var baseEnemyMultiplier = math.lerp(1f, config.MaxEnemyStatMultiplier, timeFactor);
            var baseSpawnMultiplier = math.lerp(1f, config.MaxSpawnRateMultiplier, timeFactor);

            var levelBonus = 1f;
            foreach (var stats in SystemAPI.Query<RefRO<PlayerStats>>().WithAll<PlayerTag>())
            {
                var level = stats.ValueRO.Level;
                levelBonus = 1f + math.max(0, level - 1) * config.LevelStatBonus;
                break;
            }

            difficulty.ValueRW.EnemyMultiplier = baseEnemyMultiplier * levelBonus;
            difficulty.ValueRW.SpawnRateMultiplier = baseSpawnMultiplier * (1f + (levelBonus - 1f) * 0.5f);
        }
    }
}
