using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    [CreateAssetMenu(menuName = "Config/Progression")]
    public class ProgressionConfigSO : ScriptableObject, IConfigApplier
    {
        [Header("Прогрессия уровней")]
        public int BaseXpToLevel = 10;
        public int XpIncreasePerLevel = 5;

        [Header("Сложность")]
        public float TimeToMaxDifficulty = 300f;
        public float MaxEnemyStatMultiplier = 2f;
        public float MaxSpawnRateMultiplier = 2f;
        public float LevelStatBonus = 0.05f;

        public void Apply(Entity entity, EntityManager em)
        {
            em.AddComponentData(entity, new LevelConfigData
            {
                BaseXpToLevel = BaseXpToLevel,
                XpIncreasePerLevel = XpIncreasePerLevel
            });

            em.AddComponentData(entity, new DifficultyConfigData
            {
                TimeToMaxDifficulty = TimeToMaxDifficulty,
                MaxEnemyStatMultiplier = MaxEnemyStatMultiplier,
                MaxSpawnRateMultiplier = MaxSpawnRateMultiplier,
                LevelStatBonus = LevelStatBonus
            });
        }
    }
}
