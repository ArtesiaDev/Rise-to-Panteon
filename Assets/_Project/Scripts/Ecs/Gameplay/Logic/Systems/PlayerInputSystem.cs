namespace RuntimeRoguelike.Ecs
{
    public class PlayerInputSystem : IEcsUpdateSystem
    {
        private readonly IInputService _inputService;

        public PlayerInputSystem(IInputService inputService)
        {
            _inputService = inputService;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            if (counters.PlayerEntityId < 0)
            {
                return;
            }

            var input = _inputService.Read();
            if (world.TryGetResource<RunCommands>(out var commands) && input.RestartPressed)
            {
                commands.RestartRequested = true;
            }

            var moveIntentPool = world.GetPool<MoveIntent>();
            if (input.MoveDirection != Int2.Zero)
            {
                moveIntentPool.Add(counters.PlayerEntityId).Direction = input.MoveDirection;
            }
            else
            {
                moveIntentPool.RemoveEntity(counters.PlayerEntityId);
            }

            if (input.AttackPressed)
            {
                world.GetPool<AttackIntent>().Add(counters.PlayerEntityId);
            }
        }
    }
}
