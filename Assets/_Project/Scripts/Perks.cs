using UnityEngine;

namespace RuntimeRoguelike
{
    public enum PerkType
    {
        MaxHp,
        Damage,
        MoveSpeed
    }

    public readonly struct PerkDefinition
    {
        public readonly PerkType Type;
        public readonly string Title;
        public readonly string Description;
        public readonly float Value;

        public PerkDefinition(PerkType type, string title, string description, float value)
        {
            Type = type;
            Title = title;
            Description = description;
            Value = value;
        }
    }

    public static class PerkLibrary
    {
        public static readonly PerkDefinition[] All =
        {
            new PerkDefinition(PerkType.MaxHp, "Vitality", "+5 Max HP", 5f),
            new PerkDefinition(PerkType.Damage, "Strength", "+1 Damage", 1f),
            new PerkDefinition(PerkType.MoveSpeed, "Agility", "+0.2 Move Speed", 0.2f)
        };

        public static PerkDefinition[] PickRandom(int count, System.Random rng)
        {
            count = Mathf.Clamp(count, 1, All.Length);
            var indices = new int[All.Length];
            for (var i = 0; i < All.Length; i++)
            {
                indices[i] = i;
            }

            for (var i = 0; i < All.Length; i++)
            {
                var swap = rng.Next(i, All.Length);
                var temp = indices[i];
                indices[i] = indices[swap];
                indices[swap] = temp;
            }

            var result = new PerkDefinition[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = All[indices[i]];
            }

            return result;
        }
    }
}
