using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct MapGenerationConfigData : IComponentData
    {
        public int2 MapSize;
        public int MinRoomSize;
        public int MaxRoomSize;
        public int SafeRadius;
        public int2 FallbackRoomSize;
        /// <summary>Вероятность составной формы комнаты (0.0–1.0).</summary>
        public float CompositeRoomChance;
        /// <summary>Доля дополнительных рёбер поверх MST (0.0–1.0).</summary>
        public float ExtraCorridorRatio;
        /// <summary>Минимальная ширина коридора в клетках.</summary>
        public int MinCorridorWidth;
        /// <summary>Максимальная ширина коридора в клетках.</summary>
        public int MaxCorridorWidth;
        /// <summary>Коэффициент плотности заполнения карты (0.5–1.0).</summary>
        public float DensityFactor;
        /// <summary>Количество визуальных вариантов пола.</summary>
        public int FloorVariantCount;
    }
}
