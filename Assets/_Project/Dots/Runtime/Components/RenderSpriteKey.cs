using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public enum DotsSpriteKey
    {
        Player,
        Enemy,
        Loot
    }

    public struct SpriteKeyComponent : IComponentData
    {
        public DotsSpriteKey Value;
    }
}
