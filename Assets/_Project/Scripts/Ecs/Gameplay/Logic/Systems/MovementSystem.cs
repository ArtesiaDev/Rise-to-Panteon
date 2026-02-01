namespace RuntimeRoguelike.Ecs
{
    public class MovementSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<MapGrid>(out var mapGrid))
            {
                return;
            }

            if (!world.TryGetResource<GridOccupancy>(out var occupancy))
            {
                return;
            }

            var positionPool = world.GetPool<GridPosition>();
            var intentPool = world.GetPool<MoveIntent>();
            var speedPool = world.GetPool<MoveSpeed>();
            var cooldownPool = world.GetPool<MoveCooldown>();
            var lastDirPool = world.GetPool<LastMoveDirection>();
            var statsPool = world.GetPool<PlayerStatsComponent>();
            var pathPool = world.GetPool<EnemyPath>();

            foreach (var entity in world.Query<GridPosition, MoveIntent, MoveSpeed, MoveCooldown>())
            {
                ref var cooldown = ref cooldownPool.GetRef(entity);
                if (cooldown.Remaining > 0f)
                {
                    continue;
                }

                var direction = intentPool.GetRef(entity).Direction;
                if (direction == Int2.Zero)
                {
                    continue;
                }

                ref var position = ref positionPool.GetRef(entity);
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

                if (lastDirPool.Has(entity))
                {
                    lastDirPool.GetRef(entity).Value = direction;
                }

                if (pathPool.Has(entity))
                {
                    ref var path = ref pathPool.GetRef(entity);
                    if (path.Steps != null && path.Index < path.Steps.Count && path.Steps[path.Index] == to)
                    {
                        path.Index += 1;
                    }
                }

                var speed = speedPool.GetRef(entity).CellsPerSecond;
                if (statsPool.Has(entity))
                {
                    speed *= statsPool.GetRef(entity).MoveSpeedMultiplier;
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
