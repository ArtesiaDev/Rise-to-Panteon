using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct PlayerAttackConfigData : IComponentData
    {
        public int BaseDamage;
        public float AttackCooldown;
        public float AttackRange;
        public float AttackOffset;
    }
}
