using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct DifficultyState : IComponentData
    {
        public float ElapsedTime;
        public float EnemyMultiplier;
        public float SpawnRateMultiplier;
    }
}
