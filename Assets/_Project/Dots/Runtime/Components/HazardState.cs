using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct HazardState : IComponentData
    {
        public HazardType Current;
        public float SpikeTickRemaining;
    }
}
