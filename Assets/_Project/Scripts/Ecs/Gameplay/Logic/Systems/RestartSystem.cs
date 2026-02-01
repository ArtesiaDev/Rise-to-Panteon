namespace RuntimeRoguelike.Ecs
{
    public class RestartSystem : IEcsUpdateSystem
    {
        private readonly RunInitializer _runInitializer;
        private readonly EntityViewRegistry _registry;
        private readonly EntityViewPool _pool;
        private readonly WorldRoots _roots;

        public RestartSystem(RunInitializer runInitializer, EntityViewRegistry registry, EntityViewPool pool, WorldRoots roots)
        {
            _runInitializer = runInitializer;
            _registry = registry;
            _pool = pool;
            _roots = roots;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<RunCommands>(out var commands))
            {
                return;
            }

            if (!commands.RestartRequested)
            {
                return;
            }

            commands.RestartRequested = false;
            commandBuffer.Clear();
            ClearViews();
            _runInitializer.Initialize(world, true, true);
        }

        private void ClearViews()
        {
            if (_registry == null || _pool == null || _roots == null)
            {
                return;
            }

            foreach (var view in _registry.Views)
            {
                _pool.Release(view, _roots.PoolRoot);
            }

            _registry.Clear();
        }
    }
}
