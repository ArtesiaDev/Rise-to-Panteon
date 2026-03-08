using System;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    [CreateAssetMenu(menuName = "Config/Perk")]
    public class PerkConfigSO : ScriptableObject, IConfigApplier
    {
        [Serializable]
        public struct PerkDefinition
        {
            public PerkType Type;
            public float Value;
        }

        public int ChoicesCount = 3;
        public PerkDefinition[] Perks =
        {
            new() { Type = PerkType.MaxHp, Value = 5f },
            new() { Type = PerkType.Damage, Value = 1f },
            new() { Type = PerkType.MoveSpeed, Value = 0.2f }
        };

        public void Apply(Entity entity, EntityManager em)
        {
            em.AddComponentData(entity, new PerkConfigData
            {
                ChoicesCount = ChoicesCount
            });

            var buffer = em.AddBuffer<PerkData>(entity);
            if (Perks == null) return;

            foreach (var perk in Perks)
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
