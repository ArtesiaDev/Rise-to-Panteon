namespace RuntimeRoguelike.Ecs
{
    public class PoisonSystem : IEcsInitSystem, IEcsFixedSystem
    {
        private EcsPool<PoisonEffect> _poisonPool;
        private EcsPool<HealthComponent> _healthPool;

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _poisonPool = world.GetPool<PoisonEffect>();
            _healthPool = world.GetPool<HealthComponent>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            foreach (var entity in world.Query<PoisonEffect, HealthComponent>())
            {
                ref var poison = ref _poisonPool.GetRef(entity);
                ref var health = ref _healthPool.GetRef(entity);

                poison.Remaining -= fixedDeltaTime;
                poison.NextTick -= fixedDeltaTime;

                if (poison.NextTick <= 0f)
                {
                    poison.NextTick = poison.TickInterval;
                    var damage = EcsMath.Max(1, EcsMath.RoundToInt(poison.Dps * poison.TickInterval));
                    health.Current = EcsMath.Max(0, health.Current - damage);
                }

                if (poison.Remaining <= 0f)
                {
                    commandBuffer.RemoveComponent<PoisonEffect>(world.GetEntity(entity));
                }
            }
        }
    }
}
