using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct EnemyConfigData : IComponentData
    {
        public int MaxHealth;
        public int BaseDamage;
        public float MoveSpeed;
        public float AggroRange;
        public float AttackRange;
        public float AttackCooldown;
        public float PathRefreshInterval;
        public float IdleMoveInterval;
        public float2 ColliderSize;
    }
}
