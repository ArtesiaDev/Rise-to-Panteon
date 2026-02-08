using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct InputReadSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingletonRW<InputState>();
            var moveDir = input.ValueRO.MoveDir;
            var attackPressed = input.ValueRO.AttackPressed;

            foreach (var (_, entity) in SystemAPI
                         .Query<RefRO<PlayerTag>>()
                         .WithEntityAccess())
            {
                if (!moveDir.Equals(int2.zero))
                {
                    SystemAPI.SetComponent(entity, new MoveIntent { Direction = moveDir });
                    SystemAPI.SetComponentEnabled<MoveIntent>(entity, true);
                }

                if (attackPressed)
                {
                    SystemAPI.SetComponentEnabled<AttackRequest>(entity, true);
                }
                break;
            }

            input.ValueRW.MoveDir = int2.zero;
            input.ValueRW.AttackPressed = false;
        }
    }
}
