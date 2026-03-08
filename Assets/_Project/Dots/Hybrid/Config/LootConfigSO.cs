using System;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    [CreateAssetMenu(menuName = "Config/Loot")]
    public class LootConfigSO : ScriptableObject, IConfigApplier
    {
        [Serializable]
        public struct LootEntry
        {
            public PickupType Type;
            public int Amount;
            public int Weight;
        }

        public float DropChance = 0.45f;
        public LootEntry[] LootTable =
        {
            new() { Type = PickupType.Gold, Amount = 5, Weight = 5 },
            new() { Type = PickupType.Xp, Amount = 2, Weight = 3 },
            new() { Type = PickupType.Heal, Amount = 2, Weight = 2 }
        };

        public void Apply(Entity entity, EntityManager em)
        {
            em.AddComponentData(entity, new LootConfigData
            {
                DropChance = DropChance
            });

            var buffer = em.AddBuffer<LootEntryData>(entity);
            if (LootTable == null) return;

            foreach (var entry in LootTable)
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
