using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct RunState : IComponentData
    {
        public uint Seed;
        public int2 MapSize;
        public int2 StartCell;
        public int SafeRadius;
        public int RunId;
        public bool IsInitialized;
        public bool FixedStepApplied;
    }
}
