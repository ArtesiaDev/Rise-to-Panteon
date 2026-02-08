using System;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class LootConfigAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct LootEntryAuthoring
        {
            public PickupType Type;
            public int Amount;
            public int Weight;
        }

        [SerializeField] private float dropChance = 0.45f;
        [SerializeField] private LootEntryAuthoring[] lootTable =
        {
            new LootEntryAuthoring { Type = PickupType.Gold, Amount = 5, Weight = 5 },
            new LootEntryAuthoring { Type = PickupType.Xp, Amount = 2, Weight = 3 },
            new LootEntryAuthoring { Type = PickupType.Heal, Amount = 2, Weight = 2 }
        };

        private class Baker : Baker<LootConfigAuthoring>
        {
            public override void Bake(LootConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new LootConfigData
                {
                    DropChance = authoring.dropChance
                });

                var buffer = AddBuffer<LootEntryData>(entity);
                if (authoring.lootTable == null)
                {
                    return;
                }

                foreach (var entry in authoring.lootTable)
                {
                    buffer.Add(new LootEntryData
                    {
                        Type = entry.Type,
                        Amount = entry.Amount,
                        Weight = entry.Weight
                    });
                }
            }
        }
    }
}
