namespace RuntimeRoguelike.Ecs
{
    public class PlayerAttackSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            var positionPool = world.GetPool<GridPosition>();
            var attackPool = world.GetPool<AttackCooldown>();
            var damagePool = world.GetPool<DamageComponent>();
            var dirPool = world.GetPool<LastMoveDirection>();
            var statsPool = world.GetPool<PlayerStatsComponent>();

            var enemyPositionPool = world.GetPool<GridPosition>();
            var enemyHealthPool = world.GetPool<HealthComponent>();

            foreach (var entity in world.Query<PlayerTag, AttackIntent, AttackCooldown, DamageComponent, LastMoveDirection, GridPosition>())
            {
                ref var attackCooldown = ref attackPool.GetRef(entity);
                if (attackCooldown.Remaining > 0f)
                {
                    commandBuffer.RemoveComponent<AttackIntent>(world.GetEntity(entity));
                    continue;
                }

                var origin = positionPool.GetRef(entity).Value;
                var direction = dirPool.GetRef(entity).Value;
                var range = EcsMath.Max(1, EcsMath.RoundToInt(world.GetPool<AttackRange>().GetRef(entity).Value));

                var damage = damagePool.GetRef(entity).Value;
                if (statsPool.Has(entity))
                {
                    damage += statsPool.GetRef(entity).BonusDamage;
                }

                for (var step = 1; step <= range; step++)
                {
                    var targetCell = origin + direction * step;
                    foreach (var enemy in world.Query<EnemyTag, GridPosition, HealthComponent>())
                    {
                        if (enemyPositionPool.GetRef(enemy).Value == targetCell)
                        {
                            ref var health = ref enemyHealthPool.GetRef(enemy);
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
