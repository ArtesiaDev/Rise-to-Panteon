using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct DifficultyConfigData : IComponentData
    {
        public float TimeToMaxDifficulty;
        public float MaxEnemyStatMultiplier;
        public float MaxSpawnRateMultiplier;
        public float LevelStatBonus;
    }
}
