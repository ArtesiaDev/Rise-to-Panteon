using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct HazardState : IComponentData
    {
        public HazardType Current;
        public float SpikeTickRemaining;
    }
}
