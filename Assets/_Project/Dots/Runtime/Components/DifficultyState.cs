using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct DifficultyState : IComponentData
    {
        public float ElapsedTime;
        public float EnemyMultiplier;
        public float SpawnRateMultiplier;
    }
}
