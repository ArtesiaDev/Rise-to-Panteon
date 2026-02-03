using RuntimeRoguelike.UI.Mvp;

namespace RuntimeRoguelike.Ecs
{
    public class HudModelSyncSystem : IEcsInitSystem, IEcsUpdateSystem
    {
        private readonly HudModel _hudModel;
        private EcsPool<HealthComponent> _healthPool;
        private EcsPool<PlayerStatsComponent> _statsPool;

        public HudModelSyncSystem(HudModel hudModel)
        {
            _hudModel = hudModel;
        }

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _healthPool = world.GetPool<HealthComponent>();
            _statsPool = world.GetPool<PlayerStatsComponent>();
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            var playerId = counters.PlayerEntityId;
            if (_healthPool.Has(playerId))
            {
                var health = _healthPool.GetRef(playerId);
                _hudModel.SetHp(health.Current, health.Max);
            }

            if (_statsPool.Has(playerId))
            {
                var stats = _statsPool.GetRef(playerId);
                _hudModel.SetProgress(stats.Xp, stats.XpToNext, stats.Level);
                _hudModel.SetGold(stats.Gold);
            }

            _hudModel.SetSeed(counters.Seed);
        }
    }
}
