namespace RuntimeRoguelike.Ecs
{
    public class EnemyPathSystem : IEcsUpdateSystem
    {
        private readonly PathfindingService _pathfindingService;

        public EnemyPathSystem(PathfindingService pathfindingService)
        {
            _pathfindingService = pathfindingService;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            var positionPool = world.GetPool<GridPosition>();
            var targetPool = world.GetPool<TargetEntity>();
            var pathPool = world.GetPool<EnemyPath>();
            var refreshPool = world.GetPool<PathRefreshCooldown>();

            foreach (var entity in world.Query<EnemyTag, GridPosition, TargetEntity, EnemyPath, PathRefreshCooldown>())
            {
                ref var target = ref targetPool.GetRef(entity);
                ref var path = ref pathPool.GetRef(entity);
                ref var refresh = ref refreshPool.GetRef(entity);

                if (!target.HasTarget || !positionPool.Has(target.EntityId))
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

                var start = positionPool.GetRef(entity).Value;
                var goal = positionPool.GetRef(target.EntityId).Value;
                path.Steps = _pathfindingService.FindPath(start, goal);
                path.Index = 0;
                refresh.Remaining = refresh.Interval;
            }
        }
    }
}
