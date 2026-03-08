using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct VisibilityConfigData : IComponentData
    {
        /// <summary>Радиус видимости игрока в клетках (для symmetric shadowcasting).</summary>
        public int ViewRadius;
        /// <summary>Включён ли туман войны.</summary>
        public bool Enabled;
    }
}
