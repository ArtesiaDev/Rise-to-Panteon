namespace RuntimeRoguelike.Ecs
{
    public class CooldownSystem : IEcsInitSystem, IEcsFixedUpdateSystem
    {
        private EcsPool<MoveCooldown> _movePool;
        private EcsPool<AttackCooldown> _attackPool;
        private EcsPool<PathRefreshCooldown> _pathPool;
        private EcsPool<IdleMoveCooldown> _idlePool;

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _movePool = world.GetPool<MoveCooldown>();
            _attackPool = world.GetPool<AttackCooldown>();
            _pathPool = world.GetPool<PathRefreshCooldown>();
            _idlePool = world.GetPool<IdleMoveCooldown>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            foreach (var entity in world.Query<MoveCooldown>())
            {
                ref var cooldown = ref _movePool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    cooldown.Remaining = EcsMath.Clamp(cooldown.Remaining - fixedDeltaTime, 0f, float.MaxValue);
                }
            }

            foreach (var entity in world.Query<AttackCooldown>())
            {
                ref var cooldown = ref _attackPool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    cooldown.Remaining = EcsMath.Clamp(cooldown.Remaining - fixedDeltaTime, 0f, float.MaxValue);
                }
            }

            foreach (var entity in world.Query<PathRefreshCooldown>())
            {
                ref var cooldown = ref _pathPool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    cooldown.Remaining = EcsMath.Clamp(cooldown.Remaining - fixedDeltaTime, 0f, float.MaxValue);
                }
            }

            foreach (var entity in world.Query<IdleMoveCooldown>())
            {
                ref var cooldown = ref _idlePool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    cooldown.Remaining = EcsMath.Clamp(cooldown.Remaining - fixedDeltaTime, 0f, float.MaxValue);
                }
            }
        }
    }
}
