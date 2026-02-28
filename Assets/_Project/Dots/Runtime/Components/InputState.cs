using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct InputState : IComponentData
    {
        public int2 MoveDir;
        public bool AttackPressed;
        public bool RestartPressed;
        public bool ToggleGizmos;
        public bool TeleportRequested;
        public int2 TeleportTarget;
    }
}
