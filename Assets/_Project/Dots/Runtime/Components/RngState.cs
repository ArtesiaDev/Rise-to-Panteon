using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct RngState : IComponentData
    {
        public Random Rng;
        public bool IsInitialized;
    }
}
