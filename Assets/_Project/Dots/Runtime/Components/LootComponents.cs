using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct LootPickup : IComponentData
    {
        public PickupType Type;
        public int Amount;
    }
}
