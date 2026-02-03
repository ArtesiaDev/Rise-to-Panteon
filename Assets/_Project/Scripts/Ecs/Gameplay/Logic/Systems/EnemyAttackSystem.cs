namespace RuntimeRoguelike.Ecs
{
    public class EnemyAttackSystem : IEcsInitSystem, IEcsFixedUpdateSystem
    {
        private EcsPool<GridPosition> _positionPool;
        private EcsPool<TargetEntity> _targetPool;
        private EcsPool<AttackCooldown> _attackPool;
        private EcsPool<AttackRange> _rangePool;
        private EcsPool<DamageComponent> _damagePool;
        private EcsPool<HealthComponent> _healthPool;

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _positionPool = world.GetPool<GridPosition>();
            _targetPool = world.GetPool<TargetEntity>();
            _attackPool = world.GetPool<AttackCooldown>();
            _rangePool = world.GetPool<AttackRange>();
            _damagePool = world.GetPool<DamageComponent>();
            _healthPool = world.GetPool<HealthComponent>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            foreach (var entity in world.Query<EnemyTag, GridPosition, TargetEntity, AttackCooldown, AttackRange, DamageComponent>())
            {
                ref var target = ref _targetPool.GetRef(entity);
                if (!target.HasTarget || !_positionPool.Has(target.EntityId) || !_healthPool.Has(target.EntityId))
                {
                    target.HasTarget = false;
                    continue;
                }

                ref var cooldown = ref _attackPool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    continue;
                }

                var attackerPos = _positionPool.GetRef(entity).Value;
                var targetPos = _positionPool.GetRef(target.EntityId).Value;
                var distance = attackerPos.ManhattanDistance(targetPos);
                if (distance > _rangePool.GetRef(entity).Value)
                {
                    continue;
                }

                ref var health = ref _healthPool.GetRef(target.EntityId);
                health.Current = EcsMath.Max(0, health.Current - _damagePool.GetRef(entity).Value);
                cooldown.Remaining = cooldown.Interval;
            }
        }
    }
}
