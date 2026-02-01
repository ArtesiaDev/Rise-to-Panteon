namespace RuntimeRoguelike.Ecs
{
    public class ViewSpawnSystem : IEcsUpdateSystem
    {
        private readonly EntityViewRegistry _registry;
        private readonly EntityViewPool _pool;
        private readonly WorldRoots _roots;
        private readonly GridPositionConverter _gridPositionConverter;

        public ViewSpawnSystem(EntityViewRegistry registry, EntityViewPool pool, WorldRoots roots, GridPositionConverter gridPositionConverter)
        {
            _registry = registry;
            _pool = pool;
            _roots = roots;
            _gridPositionConverter = gridPositionConverter;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            var spritePool = world.GetPool<SpriteKeyComponent>();
            var renderPool = world.GetPool<RenderPosition>();
            var positionPool = world.GetPool<GridPosition>();
            foreach (var entity in world.Query<SpriteKeyComponent, NeedsViewTag>())
            {
                if (_registry.Contains(entity))
                {
                    commandBuffer.RemoveComponent<NeedsViewTag>(world.GetEntity(entity));
                    continue;
                }

                var spriteKey = spritePool.GetRef(entity).Value;
                var view = _pool.Get(spriteKey, _roots.EntitiesRoot);
                if (renderPool.Has(entity))
                {
                    view.Transform.position = _gridPositionConverter.CellToWorld(renderPool.GetRef(entity).Value);
                }
                else if (positionPool.Has(entity))
                {
                    var cell = positionPool.GetRef(entity).Value;
                    view.Transform.position = _gridPositionConverter.CellToWorld(new Float2(cell.X, cell.Y));
                }

                _registry.Register(entity, view);
                commandBuffer.RemoveComponent<NeedsViewTag>(world.GetEntity(entity));
            }
        }
    }
}
