using RuntimeRoguelike;
using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "LootConfig", menuName = "Configs/Loot/LootConfig", order = 7)]
    public class LootConfig : ScriptableObject
    {
        [field: SerializeField] public float DropChance { get; private set; } = 0.45f;
        [field: SerializeField] public LootEntry[] LootTable { get; private set; } =
        {
            new LootEntry { Type = PickupType.Gold, Amount = 2, Weight = 6 },
            new LootEntry { Type = PickupType.Xp, Amount = 3, Weight = 4 },
            new LootEntry { Type = PickupType.Heal, Amount = 2, Weight = 2 }
        };
    }
}
