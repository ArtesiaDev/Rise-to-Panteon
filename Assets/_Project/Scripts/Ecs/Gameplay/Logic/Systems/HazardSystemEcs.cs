using RuntimeRoguelike.Configs;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class HazardSystemEcs : IEcsInitSystem, IEcsFixedUpdateSystem
    {
        private readonly HazardConfig _config;
        private EcsPool<GridPosition> _positionPool;
        private EcsPool<HealthComponent> _healthPool;
        private EcsPool<HazardState> _hazardStatePool;
        private EcsPool<PoisonEffect> _poisonPool;

        public HazardSystemEcs(HazardConfig config)
        {
            _config = config;
        }

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _positionPool = world.GetPool<GridPosition>();
            _healthPool = world.GetPool<HealthComponent>();
            _hazardStatePool = world.GetPool<HazardState>();
            _poisonPool = world.GetPool<PoisonEffect>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            if (!world.TryGetResource<MapGrid>(out var mapGrid))
            {
                return;
            }

            foreach (var entity in world.Query<GridPosition, HealthComponent, HazardState>())
            {
                ref var position = ref _positionPool.GetRef(entity);
                ref var hazardState = ref _hazardStatePool.GetRef(entity);
                ref var health = ref _healthPool.GetRef(entity);

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
                        ref var poison = ref _poisonPool.Add(entity);
                        poison.Remaining = EcsMath.Max(poison.Remaining, _config.PoisonDuration);
                        poison.Dps = EcsMath.Max(poison.Dps, _config.PoisonDps);
                        poison.TickInterval = EcsMath.Clamp(_config.PoisonTickInterval, 0.05f, 10f);
                        poison.NextTick = poison.TickInterval;
                    }
                }

                if (hazardState.Current == HazardType.Spike)
                {
                    hazardState.SpikeTickRemaining -= fixedDeltaTime;
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
