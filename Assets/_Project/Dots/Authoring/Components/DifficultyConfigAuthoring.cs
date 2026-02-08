using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class DifficultyConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private float timeToMaxDifficulty = 300f;
        [SerializeField] private float maxEnemyStatMultiplier = 2f;
        [SerializeField] private float maxSpawnRateMultiplier = 2f;
        [SerializeField] private float levelStatBonus = 0.05f;

        private class Baker : Baker<DifficultyConfigAuthoring>
        {
            public override void Bake(DifficultyConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new DifficultyConfigData
                {
                    TimeToMaxDifficulty = authoring.timeToMaxDifficulty,
                    MaxEnemyStatMultiplier = authoring.maxEnemyStatMultiplier,
                    MaxSpawnRateMultiplier = authoring.maxSpawnRateMultiplier,
                    LevelStatBonus = authoring.levelStatBonus
                });
            }
        }
    }
}
