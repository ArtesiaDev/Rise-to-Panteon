using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "Configs/Entities/EnemySpawnConfig", order = 4)]
    public class EnemySpawnConfig : ScriptableObject
    {
        [field: SerializeField] public int InitialCount { get; private set; } = 12;
        [field: SerializeField] public int MaxCount { get; private set; } = 40;
        [field: SerializeField] public float SpawnInterval { get; private set; } = 6f;
        [field: SerializeField] public int SpawnAttempts { get; private set; } = 200;
    }
}
