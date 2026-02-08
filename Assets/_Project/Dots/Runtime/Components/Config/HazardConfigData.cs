using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct HazardConfigData : IComponentData
    {
        public float HazardChance;
        public float SpikeChance;
        public float SpikeTickInterval;
        public int SpikeDamage;
        public float PoisonDuration;
        public float PoisonDps;
        public float PoisonTickInterval;
    }
}
