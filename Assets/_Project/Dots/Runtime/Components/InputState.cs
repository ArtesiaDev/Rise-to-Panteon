using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    public struct InputState : IComponentData
    {
        public int2 MoveDir;
        public bool AttackPressed;
        public bool RestartPressed;
        public bool ToggleGizmos;
        public bool Teleport;
    }
}
