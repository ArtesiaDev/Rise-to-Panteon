using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct Health : IComponentData
    {
        public int Current;
        public int Max;
    }

    public struct Damage : IComponentData
    {
        public int Value;
    }

    public struct AttackCooldown : IComponentData
    {
        public float Remaining;
        public float Interval;
    }

    public struct AttackRange : IComponentData
    {
        public float Value;
    }

    public struct AttackRequest : IComponentData, IEnableableComponent
    {
    }
}
