using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct PlayerConfigData : IComponentData
    {
        public int MaxHealth;
        public float MoveSpeed;
    }
}
