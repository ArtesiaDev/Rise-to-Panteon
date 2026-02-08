using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class MapGenerationConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int _roomAttempts = 60;
        [SerializeField] private int _minRoomSize = 6;
        [SerializeField] private int _maxRoomSize = 14;
        [SerializeField] private int _safeRadius = 6;
        [SerializeField] private Vector2Int _fallbackRoomSize = new(8, 8);

        private class Baker : Baker<MapGenerationConfigAuthoring>
        {
            public override void Bake(MapGenerationConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MapGenerationConfigData
                {
                    RoomAttempts = authoring._roomAttempts,
                    MinRoomSize = authoring._minRoomSize,
                    MaxRoomSize = authoring._maxRoomSize,
                    SafeRadius = authoring._safeRadius,
                    FallbackRoomSize = new int2(authoring._fallbackRoomSize.x, authoring._fallbackRoomSize.y)
                });
            }
        }
    }
}
