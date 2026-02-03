namespace RuntimeRoguelike.Ecs
{
    public class ViewSyncSystem : IEcsInitSystem, IEcsLateUpdateSystem
    {
        private readonly EntityViewRegistry _registry;
        private readonly GridPositionConverter _gridPositionConverter;
        private EcsPool<GridPosition> _positionPool;
        private EcsPool<RenderPosition> _renderPool;
        private EcsPool<MoveSpeed> _movePool;
        private EcsPool<PlayerStatsComponent> _statsPool;

        public ViewSyncSystem(EntityViewRegistry registry, GridPositionConverter gridPositionConverter)
        {
            _registry = registry;
            _gridPositionConverter = gridPositionConverter;
        }

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _positionPool = world.GetPool<GridPosition>();
            _renderPool = world.GetPool<RenderPosition>();
            _movePool = world.GetPool<MoveSpeed>();
            _statsPool = world.GetPool<PlayerStatsComponent>();
        }

        public void LateUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            foreach (var entity in world.Query<GridPosition, RenderPosition, SpriteKeyComponent>())
            {
                if (!_registry.TryGet(entity, out var view))
                {
                    continue;
                }

                ref var grid = ref _positionPool.GetRef(entity);
                ref var render = ref _renderPool.GetRef(entity);

                var target = new Float2(grid.Value.X, grid.Value.Y);
                var speed = _movePool.Has(entity) ? _movePool.GetRef(entity).CellsPerSecond : 0f;
                if (_statsPool.Has(entity))
                {
                    speed *= _statsPool.GetRef(entity).MoveSpeedMultiplier;
                }

                render.Value = speed > 0.01f
                    ? EcsMath.MoveTowards(render.Value, target, speed * deltaTime)
                    : target;

                view.Transform.position = _gridPositionConverter.CellToWorld(render.Value);
            }
        }
    }
}
