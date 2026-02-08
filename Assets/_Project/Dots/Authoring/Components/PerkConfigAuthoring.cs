using System;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class PerkConfigAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct PerkDefinitionAuthoring
        {
            public PerkType Type;
            public float Value;
        }

        [SerializeField] private int choicesCount = 3;
        [SerializeField] private PerkDefinitionAuthoring[] perks =
        {
            new PerkDefinitionAuthoring { Type = PerkType.MaxHp, Value = 5f },
            new PerkDefinitionAuthoring { Type = PerkType.Damage, Value = 1f },
            new PerkDefinitionAuthoring { Type = PerkType.MoveSpeed, Value = 0.2f }
        };

        private class Baker : Baker<PerkConfigAuthoring>
        {
            public override void Bake(PerkConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PerkConfigData
                {
                    ChoicesCount = authoring.choicesCount
                });

                var buffer = AddBuffer<PerkData>(entity);
                if (authoring.perks == null)
                {
                    return;
                }

                foreach (var perk in authoring.perks)
                {
                    buffer.Add(new PerkData
                    {
                        Type = perk.Type,
                        Value = perk.Value
                    });
                }
            }
        }
    }
}
