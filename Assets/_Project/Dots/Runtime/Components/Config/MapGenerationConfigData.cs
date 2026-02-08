using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    public struct MapGenerationConfigData : IComponentData
    {
        public int RoomAttempts;
        public int MinRoomSize;
        public int MaxRoomSize;
        public int SafeRadius;
        public int2 FallbackRoomSize;
    }
}
