namespace RuntimeRoguelike.Ecs
{
    public class PlayerAttackSystem : IEcsInitSystem, IEcsFixedSystem
    {
        private EcsPool<GridPosition> _positionPool;
        private EcsPool<AttackCooldown> _attackPool;
        private EcsPool<DamageComponent> _damagePool;
        private EcsPool<LastMoveDirection> _dirPool;
        private EcsPool<PlayerStatsComponent> _statsPool;
        private EcsPool<GridPosition> _enemyPositionPool;
        private EcsPool<HealthComponent> _enemyHealthPool;
        private EcsPool<AttackRange> _attackRangePool;

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _positionPool = world.GetPool<GridPosition>();
            _attackPool = world.GetPool<AttackCooldown>();
            _damagePool = world.GetPool<DamageComponent>();
            _dirPool = world.GetPool<LastMoveDirection>();
            _statsPool = world.GetPool<PlayerStatsComponent>();
            _enemyPositionPool = world.GetPool<GridPosition>();
            _enemyHealthPool = world.GetPool<HealthComponent>();
            _attackRangePool = world.GetPool<AttackRange>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            foreach (var entity in world.Query<PlayerTag, AttackIntent, AttackCooldown, DamageComponent, LastMoveDirection, GridPosition, AttackRange>())
            {
                ref var attackCooldown = ref _attackPool.GetRef(entity);
                if (attackCooldown.Remaining > 0f)
                {
                    commandBuffer.RemoveComponent<AttackIntent>(world.GetEntity(entity));
                    continue;
                }

                var origin = _positionPool.GetRef(entity).Value;
                var direction = _dirPool.GetRef(entity).Value;
                var range = EcsMath.Max(1, EcsMath.RoundToInt(_attackRangePool.GetRef(entity).Value));

                var damage = _damagePool.GetRef(entity).Value;
                if (_statsPool.Has(entity))
                {
                    damage += _statsPool.GetRef(entity).BonusDamage;
                }

                for (var step = 1; step <= range; step++)
                {
                    var targetCell = origin + direction * step;
                    foreach (var enemy in world.Query<EnemyTag, GridPosition, HealthComponent>())
                    {
                        if (_enemyPositionPool.GetRef(enemy).Value == targetCell)
                        {
                            ref var health = ref _enemyHealthPool.GetRef(enemy);
                            health.Current = EcsMath.Max(0, health.Current - damage);
                        }
                    }
                }

                attackCooldown.Remaining = attackCooldown.Interval;
                commandBuffer.RemoveComponent<AttackIntent>(world.GetEntity(entity));
            }
        }
    }
}
