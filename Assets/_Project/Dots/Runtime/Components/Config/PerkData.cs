using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct PerkData : IBufferElementData
    {
        public PerkType Type;
        public float Value;
    }
}
