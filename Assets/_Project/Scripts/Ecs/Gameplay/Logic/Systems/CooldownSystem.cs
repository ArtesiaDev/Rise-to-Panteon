namespace RuntimeRoguelike.Ecs
{
    public class CooldownSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            var movePool = world.GetPool<MoveCooldown>();
            foreach (var entity in world.Query<MoveCooldown>())
            {
                ref var cooldown = ref movePool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    cooldown.Remaining = EcsMath.Clamp(cooldown.Remaining - deltaTime, 0f, float.MaxValue);
                }
            }

            var attackPool = world.GetPool<AttackCooldown>();
            foreach (var entity in world.Query<AttackCooldown>())
            {
                ref var cooldown = ref attackPool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    cooldown.Remaining = EcsMath.Clamp(cooldown.Remaining - deltaTime, 0f, float.MaxValue);
                }
            }

            var pathPool = world.GetPool<PathRefreshCooldown>();
            foreach (var entity in world.Query<PathRefreshCooldown>())
            {
                ref var cooldown = ref pathPool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    cooldown.Remaining = EcsMath.Clamp(cooldown.Remaining - deltaTime, 0f, float.MaxValue);
                }
            }

            var idlePool = world.GetPool<IdleMoveCooldown>();
            foreach (var entity in world.Query<IdleMoveCooldown>())
            {
                ref var cooldown = ref idlePool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    cooldown.Remaining = EcsMath.Clamp(cooldown.Remaining - deltaTime, 0f, float.MaxValue);
                }
            }
        }
    }
}
