namespace RuntimeRoguelike.Ecs
{
    public class ViewSyncSystem : IEcsLateSystem
    {
        private readonly EntityViewRegistry _registry;
        private readonly GridPositionConverter _gridPositionConverter;

        public ViewSyncSystem(EntityViewRegistry registry, GridPositionConverter gridPositionConverter)
        {
            _registry = registry;
            _gridPositionConverter = gridPositionConverter;
        }

        public void LateUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            var positionPool = world.GetPool<GridPosition>();
            var renderPool = world.GetPool<RenderPosition>();
            var movePool = world.GetPool<MoveSpeed>();
            var statsPool = world.GetPool<PlayerStatsComponent>();

            foreach (var entity in world.Query<GridPosition, RenderPosition, SpriteKeyComponent>())
            {
                if (!_registry.TryGet(entity, out var view))
                {
                    continue;
                }

                ref var grid = ref positionPool.GetRef(entity);
                ref var render = ref renderPool.GetRef(entity);

                var target = new Float2(grid.Value.X, grid.Value.Y);
                var speed = movePool.Has(entity) ? movePool.GetRef(entity).CellsPerSecond : 0f;
                if (statsPool.Has(entity))
                {
                    speed *= statsPool.GetRef(entity).MoveSpeedMultiplier;
                }

                render.Value = speed > 0.01f
                    ? EcsMath.MoveTowards(render.Value, target, speed * deltaTime)
                    : target;

                view.Transform.position = _gridPositionConverter.CellToWorld(render.Value);
            }
        }
    }
}
