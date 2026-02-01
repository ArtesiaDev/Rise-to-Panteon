using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "PlayerAttackConfig", menuName = "Configs/Entities/PlayerAttackConfig", order = 6)]
    public class PlayerAttackConfig : ScriptableObject
    {
        [field: SerializeField] public int BaseDamage { get; private set; } = 2;
        [field: SerializeField] public float AttackCooldown { get; private set; } = 0.4f;
        [field: SerializeField] public float AttackRange { get; private set; } = 0.7f;
        [field: SerializeField] public float AttackOffset { get; private set; } = 0.6f;
        [field: SerializeField] public KeyCode AttackKey { get; private set; } = KeyCode.Space;
    }
}
