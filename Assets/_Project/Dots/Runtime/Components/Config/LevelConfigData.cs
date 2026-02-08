using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct LevelConfigData : IComponentData
    {
        public int BaseXpToLevel;
        public int XpIncreasePerLevel;
    }
}
