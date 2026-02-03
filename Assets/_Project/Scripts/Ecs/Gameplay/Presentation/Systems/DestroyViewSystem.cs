namespace RuntimeRoguelike.Ecs
{
    public class DestroyViewSystem : IEcsLateSystem, IEcsDisposeSystem
    {
        private readonly EntityViewRegistry _registry;
        private readonly EntityViewPool _pool;
        private readonly WorldRoots _roots;

        public DestroyViewSystem(EntityViewRegistry registry, EntityViewPool pool, WorldRoots roots)
        {
            _registry = registry;
            _pool = pool;
            _roots = roots;
        }

        public void LateUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            foreach (var entity in world.Query<DestroyedTag>())
            {
                if (_registry.Unregister(entity, out var view))
                {
                    _pool.Release(view, _roots.PoolRoot);
                }

                commandBuffer.DestroyEntity(world.GetEntity(entity));
            }
        }

        public void Dispose(EcsWorld world)
        {
            foreach (var view in _registry.Views)
            {
                _pool.Release(view, _roots.PoolRoot);
            }

            _registry.Clear();
        }
    }
}
