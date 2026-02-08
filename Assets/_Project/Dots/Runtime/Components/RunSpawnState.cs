using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct RunSpawnState : IComponentData
    {
        public bool PlayerSpawned;
        public bool InitialEnemiesSpawned;
    }
}
