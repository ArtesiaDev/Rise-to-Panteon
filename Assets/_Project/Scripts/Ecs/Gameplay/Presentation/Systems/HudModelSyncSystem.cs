using RuntimeRoguelike.UI.Mvp;

namespace RuntimeRoguelike.Ecs
{
    public class HudModelSyncSystem : IEcsUpdateSystem
    {
        private readonly HudModel _hudModel;

        public HudModelSyncSystem(HudModel hudModel)
        {
            _hudModel = hudModel;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            var playerId = counters.PlayerEntityId;
            var healthPool = world.GetPool<HealthComponent>();
            var statsPool = world.GetPool<PlayerStatsComponent>();

            if (healthPool.Has(playerId))
            {
                var health = healthPool.GetRef(playerId);
                _hudModel.SetHp(health.Current, health.Max);
            }

            if (statsPool.Has(playerId))
            {
                var stats = statsPool.GetRef(playerId);
                _hudModel.SetProgress(stats.Xp, stats.XpToNext, stats.Level);
                _hudModel.SetGold(stats.Gold);
            }

            _hudModel.SetSeed(counters.Seed);
        }
    }
}
