using System;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class LootConfigAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct LootEntryAuthoring
        {
            public PickupType _type;
            public int _amount;
            public int _weight;
        }

        [SerializeField] private float _dropChance = 0.45f;
        [SerializeField] private LootEntryAuthoring[] _lootTable =
        {
            new() { _type = PickupType.Gold, _amount = 5, _weight = 5 },
            new() { _type = PickupType.Xp, _amount = 2, _weight = 3 },
            new() { _type = PickupType.Heal, _amount = 2, _weight = 2 }
        };

        private class Baker : Baker<LootConfigAuthoring>
        {
            public override void Bake(LootConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new LootConfigData
                {
                    DropChance = authoring._dropChance
                });

                var buffer = AddBuffer<LootEntryData>(entity);
                if (authoring._lootTable == null)
                {
                    return;
                }

                foreach (var entry in authoring._lootTable)
                {
                    buffer.Add(new LootEntryData
                    {
                        Type = entry._type,
                        Amount = entry._amount,
                        Weight = entry._weight
                    });
                }
            }
        }
    }
}
