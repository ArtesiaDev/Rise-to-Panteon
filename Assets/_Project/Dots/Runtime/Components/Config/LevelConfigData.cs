using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct LevelConfigData : IComponentData
    {
        public int BaseXpToLevel;
        public int XpIncreasePerLevel;
    }
}
