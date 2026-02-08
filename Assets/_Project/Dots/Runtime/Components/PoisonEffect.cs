using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct PoisonEffect : IComponentData
    {
        public float Remaining;
        public float Dps;
        public float TickInterval;
        public float NextTick;
    }
}
