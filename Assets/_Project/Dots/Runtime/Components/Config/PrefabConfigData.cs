using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct PrefabConfigData : IComponentData
    {
        public Entity Player;
        public Entity Enemy;
        public Entity Loot;
    }
}
