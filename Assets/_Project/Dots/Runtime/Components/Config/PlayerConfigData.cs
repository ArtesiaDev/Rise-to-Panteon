using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    public struct PlayerConfigData : IComponentData
    {
        public int MaxHealth;
        public float MoveSpeed;
        public float2 ColliderSize;
    }
}
