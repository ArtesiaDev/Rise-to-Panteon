using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/Entities/EnemyConfig", order = 3)]
    public class EnemyConfig : ScriptableObject
    {
        [field: SerializeField] public int MaxHealth { get; private set; } = 4;
        [field: SerializeField] public int BaseDamage { get; private set; } = 1;
        [field: SerializeField] public float MoveSpeed { get; private set; } = 3f;
        [field: SerializeField] public float AggroRange { get; private set; } = 8f;
        [field: SerializeField] public float AttackRange { get; private set; } = 1.1f;
        [field: SerializeField] public float AttackCooldown { get; private set; } = 1.2f;
        [field: SerializeField] public float PathRefreshInterval { get; private set; } = 0.6f;
        [field: SerializeField] public float IdleMoveInterval { get; private set; } = 2.5f;
        [field: SerializeField] public Vector2 ColliderSize { get; private set; } = new Vector2(0.8f, 0.8f);
    }
}
