using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class MapGenerationConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int roomAttempts = 60;
        [SerializeField] private int minRoomSize = 6;
        [SerializeField] private int maxRoomSize = 14;
        [SerializeField] private int safeRadius = 6;
        [SerializeField] private Vector2Int fallbackRoomSize = new Vector2Int(8, 8);

        private class Baker : Baker<MapGenerationConfigAuthoring>
        {
            public override void Bake(MapGenerationConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MapGenerationConfigData
                {
                    RoomAttempts = authoring.roomAttempts,
                    MinRoomSize = authoring.minRoomSize,
                    MaxRoomSize = authoring.maxRoomSize,
                    SafeRadius = authoring.safeRadius,
                    FallbackRoomSize = new int2(authoring.fallbackRoomSize.x, authoring.fallbackRoomSize.y)
                });
            }
        }
    }
}
