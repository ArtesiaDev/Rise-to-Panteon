using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct AggroRange : IComponentData
    {
        public float Value;
    }

    public struct Target : IComponentData
    {
        public Entity Value;
        public bool HasTarget;
    }

    public struct PathRefreshCooldown : IComponentData
    {
        public float Remaining;
        public float Interval;
    }

    public struct IdleMoveCooldown : IComponentData
    {
        public float Remaining;
        public float Interval;
    }

    public struct PathIndex : IComponentData
    {
        public int Value;
    }

    public struct PathStep : IBufferElementData
    {
        public int2 Value;
    }
}
