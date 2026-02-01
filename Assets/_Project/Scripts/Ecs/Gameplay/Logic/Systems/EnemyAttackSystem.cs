namespace RuntimeRoguelike.Ecs
{
    public class EnemyAttackSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            var positionPool = world.GetPool<GridPosition>();
            var targetPool = world.GetPool<TargetEntity>();
            var attackPool = world.GetPool<AttackCooldown>();
            var rangePool = world.GetPool<AttackRange>();
            var damagePool = world.GetPool<DamageComponent>();
            var healthPool = world.GetPool<HealthComponent>();

            foreach (var entity in world.Query<EnemyTag, GridPosition, TargetEntity, AttackCooldown, AttackRange, DamageComponent>())
            {
                ref var target = ref targetPool.GetRef(entity);
                if (!target.HasTarget || !positionPool.Has(target.EntityId) || !healthPool.Has(target.EntityId))
                {
                    target.HasTarget = false;
                    continue;
                }

                ref var cooldown = ref attackPool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    continue;
                }

                var attackerPos = positionPool.GetRef(entity).Value;
                var targetPos = positionPool.GetRef(target.EntityId).Value;
                var distance = attackerPos.ManhattanDistance(targetPos);
                if (distance > rangePool.GetRef(entity).Value)
                {
                    continue;
                }

                ref var health = ref healthPool.GetRef(target.EntityId);
                health.Current = EcsMath.Max(0, health.Current - damagePool.GetRef(entity).Value);
                cooldown.Remaining = cooldown.Interval;
            }
        }
    }
}
