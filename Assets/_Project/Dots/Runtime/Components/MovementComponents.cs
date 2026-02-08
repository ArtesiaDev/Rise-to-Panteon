using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    public struct GridPosition : IComponentData
    {
        public int2 Value;
    }

    public struct RenderPosition : IComponentData
    {
        public float2 Value;
    }

    public struct MoveIntent : IComponentData, IEnableableComponent
    {
        public int2 Direction;
    }

    public struct MoveSpeed : IComponentData
    {
        public float CellsPerSecond;
    }

    public struct MoveCooldown : IComponentData
    {
        public float Remaining;
    }

    public struct LastMoveDirection : IComponentData
    {
        public int2 Value;
    }
}
