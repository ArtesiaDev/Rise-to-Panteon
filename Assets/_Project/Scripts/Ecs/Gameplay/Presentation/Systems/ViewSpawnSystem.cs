namespace RuntimeRoguelike.Ecs
{
    public class ViewSpawnSystem : IEcsInitSystem, IEcsUpdateSystem
    {
        private readonly EntityViewRegistry _registry;
        private readonly EntityViewPool _pool;
        private readonly WorldRoots _roots;
        private readonly GridPositionConverter _gridPositionConverter;
        private EcsPool<SpriteKeyComponent> _spritePool;
        private EcsPool<RenderPosition> _renderPool;
        private EcsPool<GridPosition> _positionPool;

        public ViewSpawnSystem(EntityViewRegistry registry, EntityViewPool pool, WorldRoots roots, GridPositionConverter gridPositionConverter)
        {
            _registry = registry;
            _pool = pool;
            _roots = roots;
            _gridPositionConverter = gridPositionConverter;
        }

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _spritePool = world.GetPool<SpriteKeyComponent>();
            _renderPool = world.GetPool<RenderPosition>();
            _positionPool = world.GetPool<GridPosition>();
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            foreach (var entity in world.Query<SpriteKeyComponent, NeedsViewTag>())
            {
                if (_registry.Contains(entity))
                {
                    commandBuffer.RemoveComponent<NeedsViewTag>(world.GetEntity(entity));
                    continue;
                }

                var spriteKey = _spritePool.GetRef(entity).Value;
                var view = _pool.Get(spriteKey, _roots.EntitiesRoot);
                if (_renderPool.Has(entity))
                {
                    view.Transform.position = _gridPositionConverter.CellToWorld(_renderPool.GetRef(entity).Value);
                }
                else if (_positionPool.Has(entity))
                {
                    var cell = _positionPool.GetRef(entity).Value;
                    view.Transform.position = _gridPositionConverter.CellToWorld(new Float2(cell.X, cell.Y));
                }

                _registry.Register(entity, view);
                commandBuffer.RemoveComponent<NeedsViewTag>(world.GetEntity(entity));
            }
        }
    }
}
