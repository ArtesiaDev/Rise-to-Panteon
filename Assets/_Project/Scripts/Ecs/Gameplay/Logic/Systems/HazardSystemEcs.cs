using RuntimeRoguelike.Configs;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class HazardSystemEcs : IEcsUpdateSystem
    {
        private readonly HazardConfig _config;

        public HazardSystemEcs(HazardConfig config)
        {
            _config = config;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<MapGrid>(out var mapGrid))
            {
                return;
            }

            var positionPool = world.GetPool<GridPosition>();
            var healthPool = world.GetPool<HealthComponent>();
            var hazardStatePool = world.GetPool<HazardState>();
            var poisonPool = world.GetPool<PoisonEffect>();

            foreach (var entity in world.Query<GridPosition, HealthComponent, HazardState>())
            {
                ref var position = ref positionPool.GetRef(entity);
                ref var hazardState = ref hazardStatePool.GetRef(entity);
                ref var health = ref healthPool.GetRef(entity);

                var hazard = mapGrid.Get(position.Value).Hazard;
                if (hazardState.Current != hazard)
                {
                    hazardState.Current = hazard;
                    hazardState.SpikeTickRemaining = 0f;

                    if (hazard == HazardType.Spike)
                    {
                        health.Current = EcsMath.Max(0, health.Current - _config.SpikeDamage);
                        hazardState.SpikeTickRemaining = _config.SpikeTickInterval;
                    }
                    else if (hazard == HazardType.Poison)
                    {
                        ref var poison = ref poisonPool.Add(entity);
                        poison.Remaining = EcsMath.Max(poison.Remaining, _config.PoisonDuration);
                        poison.Dps = EcsMath.Max(poison.Dps, _config.PoisonDps);
                        poison.TickInterval = EcsMath.Clamp(_config.PoisonTickInterval, 0.05f, 10f);
                        poison.NextTick = poison.TickInterval;
                    }
                }

                if (hazardState.Current == HazardType.Spike)
                {
                    hazardState.SpikeTickRemaining -= deltaTime;
                    if (hazardState.SpikeTickRemaining <= 0f)
                    {
                        hazardState.SpikeTickRemaining = _config.SpikeTickInterval;
                        health.Current = EcsMath.Max(0, health.Current - _config.SpikeDamage);
                    }
                }
            }
        }
    }
}
