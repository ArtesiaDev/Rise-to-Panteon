using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct LootEntryData : IBufferElementData
    {
        public PickupType Type;
        public int Amount;
        public int Weight;
    }
}
