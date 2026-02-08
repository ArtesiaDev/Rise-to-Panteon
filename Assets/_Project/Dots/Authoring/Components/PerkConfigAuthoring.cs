using System;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class PerkConfigAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct PerkDefinitionAuthoring
        {
            public PerkType _type;
            public float _value;
        }

        [SerializeField] private int _choicesCount = 3;
        [SerializeField] private PerkDefinitionAuthoring[] _perks =
        {
            new() { _type = PerkType.MaxHp, _value = 5f },
            new() { _type = PerkType.Damage, _value = 1f },
            new() { _type = PerkType.MoveSpeed, _value = 0.2f }
        };

        private class Baker : Baker<PerkConfigAuthoring>
        {
            public override void Bake(PerkConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PerkConfigData
                {
                    ChoicesCount = authoring._choicesCount
                });

                var buffer = AddBuffer<PerkData>(entity);
                if (authoring._perks == null)
                {
                    return;
                }

                foreach (var perk in authoring._perks)
                {
                    buffer.Add(new PerkData
                    {
                        Type = perk._type,
                        Value = perk._value
                    });
                }
            }
        }
    }
}
