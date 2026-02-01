using RuntimeRoguelike.Configs;

namespace RuntimeRoguelike.Ecs
{
    public class RunInitSystem : IEcsInitSystem
    {
        private readonly RunConfig _runConfig;
        private readonly RunInitializer _runInitializer;

        public RunInitSystem(RunConfig runConfig, RunInitializer runInitializer)
        {
            _runConfig = runConfig;
            _runInitializer = runInitializer;
        }

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _runInitializer.Initialize(world, false, _runConfig.RandomizeSeedOnStart);
        }
    }
}
