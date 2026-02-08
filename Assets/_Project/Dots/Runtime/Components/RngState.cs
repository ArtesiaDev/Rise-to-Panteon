using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    public struct RngState : IComponentData
    {
        public Random Rng;
        public bool IsInitialized;
    }
}
