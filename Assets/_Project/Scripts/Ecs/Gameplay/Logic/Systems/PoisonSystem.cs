namespace RuntimeRoguelike.Ecs
{
    public class PoisonSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            var poisonPool = world.GetPool<PoisonEffect>();
            var healthPool = world.GetPool<HealthComponent>();

            foreach (var entity in world.Query<PoisonEffect, HealthComponent>())
            {
                ref var poison = ref poisonPool.GetRef(entity);
                ref var health = ref healthPool.GetRef(entity);

                poison.Remaining -= deltaTime;
                poison.NextTick -= deltaTime;

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
