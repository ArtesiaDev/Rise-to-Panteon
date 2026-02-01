using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Configs/Systems/DifficultyConfig", order = 5)]
    public class DifficultyConfig : ScriptableObject
    {
        [field: SerializeField] public float TimeToMaxDifficulty { get; private set; } = 300f;
        [field: SerializeField] public float MaxEnemyStatMultiplier { get; private set; } = 2f;
        [field: SerializeField] public float MaxSpawnRateMultiplier { get; private set; } = 2f;
        [field: SerializeField] public float LevelStatBonus { get; private set; } = 0.05f;
    }
}
