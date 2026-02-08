using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct EnemySpawnConfigData : IComponentData
    {
        public int InitialCount;
        public int MaxCount;
        public float SpawnInterval;
        public int SpawnAttempts;
    }
}
