namespace RuntimeRoguelike.Ecs
{
    public class MovementSystem : IEcsInitSystem, IEcsFixedSystem
    {
        private EcsPool<GridPosition> _positionPool;
        private EcsPool<MoveIntent> _intentPool;
        private EcsPool<MoveSpeed> _speedPool;
        private EcsPool<MoveCooldown> _cooldownPool;
        private EcsPool<LastMoveDirection> _lastDirPool;
        private EcsPool<PlayerStatsComponent> _statsPool;
        private EcsPool<EnemyPath> _pathPool;

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _positionPool = world.GetPool<GridPosition>();
            _intentPool = world.GetPool<MoveIntent>();
            _speedPool = world.GetPool<MoveSpeed>();
            _cooldownPool = world.GetPool<MoveCooldown>();
            _lastDirPool = world.GetPool<LastMoveDirection>();
            _statsPool = world.GetPool<PlayerStatsComponent>();
            _pathPool = world.GetPool<EnemyPath>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            if (!world.TryGetResource<MapGrid>(out var mapGrid))
            {
                return;
            }

            if (!world.TryGetResource<GridOccupancy>(out var occupancy))
            {
                return;
            }

            foreach (var entity in world.Query<GridPosition, MoveIntent, MoveSpeed, MoveCooldown>())
            {
                ref var cooldown = ref _cooldownPool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    continue;
                }

                var direction = _intentPool.GetRef(entity).Direction;
                if (direction == Int2.Zero)
                {
                    continue;
                }

                ref var position = ref _positionPool.GetRef(entity);
                var from = position.Value;
                var to = from + direction;

                if (!mapGrid.IsWalkable(to))
                {
                    continue;
                }

                if (!occupancy.TryMove(from, to))
                {
                    continue;
                }

                position.Value = to;

                if (_lastDirPool.Has(entity))
                {
                    _lastDirPool.GetRef(entity).Value = direction;
                }

                if (_pathPool.Has(entity))
                {
                    ref var path = ref _pathPool.GetRef(entity);
                    if (path.Steps != null && path.Index < path.Steps.Count && path.Steps[path.Index] == to)
                    {
                        path.Index += 1;
                    }
                }

                var speed = _speedPool.GetRef(entity).CellsPerSecond;
                if (_statsPool.Has(entity))
                {
                    speed *= _statsPool.GetRef(entity).MoveSpeedMultiplier;
                }

                cooldown.Remaining = speed > 0.01f ? 1f / speed : 0.1f;

                if (world.TryGetResource<RunCounters>(out var counters) && counters.PlayerEntityId == entity)
                {
                    counters.PlayerCell = to;
                }
            }
        }
    }
}
