using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct LootPickup : IComponentData
    {
        public PickupType Type;
        public int Amount;
    }
}
