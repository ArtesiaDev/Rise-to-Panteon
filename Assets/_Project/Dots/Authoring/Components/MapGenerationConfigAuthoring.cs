using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class MapGenerationConfigAuthoring : MonoBehaviour
    {
        [Header("Размер карты")]
        [SerializeField] private Vector2Int _mapSize = new(40, 55);

        [Header("Базовые параметры")]
        [SerializeField] private int _minRoomSize = 3;
        [SerializeField] private int _maxRoomSize = 7;
        [SerializeField] private int _safeRadius = 3;
        [SerializeField] private Vector2Int _fallbackRoomSize = new(6, 6);

        [Header("Расширенная генерация")]
        [SerializeField, Range(0f, 1f)] private float _compositeRoomChance = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _extraCorridorRatio = 0.2f;
        [SerializeField, Range(1, 3)] private int _minCorridorWidth = 1;
        [SerializeField, Range(1, 3)] private int _maxCorridorWidth = 1;
        [SerializeField, Range(0.1f, 1f)] private float _densityFactor = 0.35f;
        [SerializeField, Range(1, 8)] private int _floorVariantCount = 3;

        private class Baker : Baker<MapGenerationConfigAuthoring>
        {
            public override void Bake(MapGenerationConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MapGenerationConfigData
                {
                    MapSize = new int2(authoring._mapSize.x, authoring._mapSize.y),
                    MinRoomSize = authoring._minRoomSize,
                    MaxRoomSize = authoring._maxRoomSize,
                    SafeRadius = authoring._safeRadius,
                    FallbackRoomSize = new int2(authoring._fallbackRoomSize.x, authoring._fallbackRoomSize.y),
                    CompositeRoomChance = authoring._compositeRoomChance,
                    ExtraCorridorRatio = authoring._extraCorridorRatio,
                    MinCorridorWidth = authoring._minCorridorWidth,
                    MaxCorridorWidth = authoring._maxCorridorWidth,
                    DensityFactor = authoring._densityFactor,
                    FloorVariantCount = authoring._floorVariantCount
                });
            }
        }
    }
}
