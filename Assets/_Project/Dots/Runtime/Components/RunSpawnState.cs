using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct RunSpawnState : IComponentData
    {
        public bool PlayerSpawned;
        public bool InitialEnemiesSpawned;
    }
}
