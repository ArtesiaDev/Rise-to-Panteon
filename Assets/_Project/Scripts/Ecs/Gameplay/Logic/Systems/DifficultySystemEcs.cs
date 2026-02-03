using RuntimeRoguelike.Configs;

namespace RuntimeRoguelike.Ecs
{
    public class DifficultySystemEcs : IEcsInitSystem, IEcsFixedSystem
    {
        private readonly DifficultyConfig _config;
        private EcsPool<PlayerStatsComponent> _statsPool;

        public DifficultySystemEcs(DifficultyConfig config)
        {
            _config = config;
        }

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _statsPool = world.GetPool<PlayerStatsComponent>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            if (!world.TryGetResource<DifficultyState>(out var state))
            {
                return;
            }

            state.ElapsedTime += fixedDeltaTime;
            var timeFactor = EcsMath.Clamp01(state.ElapsedTime / EcsMath.Max(1f, _config.TimeToMaxDifficulty));
            var baseEnemyMultiplier = EcsMath.Lerp(1f, _config.MaxEnemyStatMultiplier, timeFactor);
            var baseSpawnMultiplier = EcsMath.Lerp(1f, _config.MaxSpawnRateMultiplier, timeFactor);

            var levelBonus = 1f;
            if (world.TryGetResource<RunCounters>(out var counters))
            {
                if (_statsPool.Has(counters.PlayerEntityId))
                {
                    var level = _statsPool.GetRef(counters.PlayerEntityId).Level;
                    levelBonus = 1f + EcsMath.Max(0, level - 1) * _config.LevelStatBonus;
                }
            }

            state.EnemyMultiplier = baseEnemyMultiplier * levelBonus;
            state.SpawnRateMultiplier = baseSpawnMultiplier * (1f + (levelBonus - 1f) * 0.5f);
        }
    }
}
