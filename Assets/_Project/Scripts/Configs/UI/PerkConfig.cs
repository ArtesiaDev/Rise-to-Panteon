using UnityEngine;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "PerkConfig", menuName = "Configs/UI/PerkConfig", order = 9)]
    public class PerkConfig : ScriptableObject
    {
        [field: SerializeField] public int ChoicesCount { get; private set; } = 3;
        [field: SerializeField] public PerkDefinition[] Perks { get; private set; } =
        {
            new PerkDefinition { Type = PerkType.MaxHp, Title = "Vitality", Description = "+5 Max HP", Value = 5f },
            new PerkDefinition { Type = PerkType.Damage, Title = "Strength", Description = "+1 Damage", Value = 1f },
            new PerkDefinition { Type = PerkType.MoveSpeed, Title = "Agility", Description = "+0.2 Move Speed", Value = 0.2f }
        };
    }
}
