namespace RuntimeRoguelike.Ecs
{
    public class EnemyTargetSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            var playerId = counters.PlayerEntityId;
            var positionPool = world.GetPool<GridPosition>();
            if (!positionPool.Has(playerId))
            {
                return;
            }

            var playerCell = positionPool.GetRef(playerId).Value;

            var aggroPool = world.GetPool<AggroRange>();
            var targetPool = world.GetPool<TargetEntity>();
            foreach (var entity in world.Query<EnemyTag, GridPosition, AggroRange, TargetEntity>())
            {
                var enemyCell = positionPool.GetRef(entity).Value;
                var distance = enemyCell.ManhattanDistance(playerCell);
                ref var target = ref targetPool.GetRef(entity);
                if (distance <= aggroPool.GetRef(entity).Value)
                {
                    target.EntityId = playerId;
                    target.HasTarget = true;
                }
                else
                {
                    target.HasTarget = false;
                }
            }
        }
    }
}
