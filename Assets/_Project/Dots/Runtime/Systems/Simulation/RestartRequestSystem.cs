using Unity.Burst;
using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RestartRequestSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RunCommand>();
            state.RequireForUpdate<InputState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingletonRW<InputState>();
            if (!input.ValueRO.RestartPressed)
            {
                return;
            }

            var command = SystemAPI.GetSingletonRW<RunCommand>();
            command.ValueRW.Restart = true;
            input.ValueRW.RestartPressed = false;
        }
    }
}
