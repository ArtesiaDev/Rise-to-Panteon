namespace RuntimeRoguelike.Ecs
{
    public class PlayerInputSystem : IEcsInitSystem, IEcsUpdateSystem
    {
        private readonly IInputService _inputService;
        private EcsPool<MoveIntent> _moveIntentPool;
        private EcsPool<AttackIntent> _attackIntentPool;

        public PlayerInputSystem(IInputService inputService)
        {
            _inputService = inputService;
        }

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _moveIntentPool = world.GetPool<MoveIntent>();
            _attackIntentPool = world.GetPool<AttackIntent>();
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

            if (input.MoveDirection != Int2.Zero)
            {
                _moveIntentPool.Add(counters.PlayerEntityId).Direction = input.MoveDirection;
            }
            else
            {
                _moveIntentPool.RemoveEntity(counters.PlayerEntityId);
            }

            if (input.AttackPressed)
            {
                _attackIntentPool.Add(counters.PlayerEntityId);
            }
        }
    }
}
