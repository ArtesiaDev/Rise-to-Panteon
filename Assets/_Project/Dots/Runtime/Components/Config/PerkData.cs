using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct PerkData : IBufferElementData
    {
        public PerkType Type;
        public float Value;
    }
}
