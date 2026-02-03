namespace RuntimeRoguelike.Ecs
{
    public class EnemyTargetSystem : IEcsInitSystem, IEcsFixedUpdateSystem
    {
        private EcsPool<GridPosition> _positionPool;
        private EcsPool<AggroRange> _aggroPool;
        private EcsPool<TargetEntity> _targetPool;

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _positionPool = world.GetPool<GridPosition>();
            _aggroPool = world.GetPool<AggroRange>();
            _targetPool = world.GetPool<TargetEntity>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            var playerId = counters.PlayerEntityId;
            if (!_positionPool.Has(playerId))
            {
                return;
            }

            var playerCell = _positionPool.GetRef(playerId).Value;

            foreach (var entity in world.Query<EnemyTag, GridPosition, AggroRange, TargetEntity>())
            {
                var enemyCell = _positionPool.GetRef(entity).Value;
                var distance = enemyCell.ManhattanDistance(playerCell);
                ref var target = ref _targetPool.GetRef(entity);
                if (distance <= _aggroPool.GetRef(entity).Value)
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
