using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    [CreateAssetMenu(menuName = "Config/Map")]
    public class MapConfigSO : ScriptableObject, IConfigApplier
    {
        [Header("Размер карты")]
        public Vector2Int MapSize = new(40, 55);
        public float CellSize = 1f;

        [Header("Параметры генерации")]
        public int MinRoomSize = 3;
        public int MaxRoomSize = 7;
        public int SafeRadius = 3;
        public Vector2Int FallbackRoomSize = new(8, 8);

        [Header("Расширенная генерация")]
        [Range(0f, 1f)] public float CompositeRoomChance = 0.3f;
        [Range(0f, 1f)] public float ExtraCorridorRatio = 0.2f;
        [Range(1, 3)] public int MinCorridorWidth = 1;
        [Range(1, 3)] public int MaxCorridorWidth = 1;
        [Range(0.1f, 1f)] public float DensityFactor = 0.35f;
        [Range(1, 8)] public int FloorVariantCount = 3;

        public void Apply(Entity entity, EntityManager em)
        {
            em.AddComponentData(entity, new GridConfigData
            {
                CellSize = CellSize
            });

            em.AddComponentData(entity, new MapGenerationConfigData
            {
                MapSize = new int2(MapSize.x, MapSize.y),
                MinRoomSize = MinRoomSize,
                MaxRoomSize = MaxRoomSize,
                SafeRadius = SafeRadius,
                FallbackRoomSize = new int2(FallbackRoomSize.x, FallbackRoomSize.y),
                CompositeRoomChance = CompositeRoomChance,
                ExtraCorridorRatio = ExtraCorridorRatio,
                MinCorridorWidth = MinCorridorWidth,
                MaxCorridorWidth = MaxCorridorWidth,
                DensityFactor = DensityFactor,
                FloorVariantCount = FloorVariantCount
            });
        }
    }
}
