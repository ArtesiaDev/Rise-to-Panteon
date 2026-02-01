using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "MapGenerationConfig", menuName = "Configs/Map/MapGenerationConfig", order = 2)]
    public class MapGenerationConfig : ScriptableObject
    {
        [field: SerializeField] public int RoomAttempts { get; private set; } = 60;
        [field: SerializeField] public int MinRoomSize { get; private set; } = 6;
        [field: SerializeField] public int MaxRoomSize { get; private set; } = 14;
        [field: SerializeField] public int SafeRadius { get; private set; } = 6;
        [field: SerializeField] public Vector2Int FallbackRoomSize { get; private set; } = new Vector2Int(8, 8);
    }
}
