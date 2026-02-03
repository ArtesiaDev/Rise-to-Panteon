namespace RuntimeRoguelike.Ecs
{
    public class EnemyPathSystem : IEcsInitSystem, IEcsFixedSystem
    {
        private readonly PathfindingService _pathfindingService;
        private EcsPool<GridPosition> _positionPool;
        private EcsPool<TargetEntity> _targetPool;
        private EcsPool<EnemyPath> _pathPool;
        private EcsPool<PathRefreshCooldown> _refreshPool;

        public EnemyPathSystem(PathfindingService pathfindingService)
        {
            _pathfindingService = pathfindingService;
        }

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _positionPool = world.GetPool<GridPosition>();
            _targetPool = world.GetPool<TargetEntity>();
            _pathPool = world.GetPool<EnemyPath>();
            _refreshPool = world.GetPool<PathRefreshCooldown>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            foreach (var entity in world.Query<EnemyTag, GridPosition, TargetEntity, EnemyPath, PathRefreshCooldown>())
            {
                ref var target = ref _targetPool.GetRef(entity);
                ref var path = ref _pathPool.GetRef(entity);
                ref var refresh = ref _refreshPool.GetRef(entity);

                if (!target.HasTarget || !_positionPool.Has(target.EntityId))
                {
                    target.HasTarget = false;
                    if (path.Steps != null)
                    {
                        path.Steps.Clear();
                    }

                    path.Index = 0;
                    continue;
                }

                if (refresh.Remaining > 0f)
                {
                    continue;
                }

                var start = _positionPool.GetRef(entity).Value;
                var goal = _positionPool.GetRef(target.EntityId).Value;
                path.Steps = _pathfindingService.FindPath(start, goal);
                path.Index = 0;
                refresh.Remaining = refresh.Interval;
            }
        }
    }
}
