using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct LootEntryData : IBufferElementData
    {
        public PickupType Type;
        public int Amount;
        public int Weight;
    }
}
