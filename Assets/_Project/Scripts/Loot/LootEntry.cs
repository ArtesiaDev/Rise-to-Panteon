using System;

namespace RuntimeRoguelike
{
    [Serializable]
    public struct LootEntry
    {
        public PickupType Type;
        public int Amount;
        public int Weight;
    }
}
