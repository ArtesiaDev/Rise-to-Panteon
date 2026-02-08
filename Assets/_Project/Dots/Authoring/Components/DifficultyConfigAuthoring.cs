using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class DifficultyConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private float _timeToMaxDifficulty = 300f;
        [SerializeField] private float _maxEnemyStatMultiplier = 2f;
        [SerializeField] private float _maxSpawnRateMultiplier = 2f;
        [SerializeField] private float _levelStatBonus = 0.05f;

        private class Baker : Baker<DifficultyConfigAuthoring>
        {
            public override void Bake(DifficultyConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new DifficultyConfigData
                {
                    TimeToMaxDifficulty = authoring._timeToMaxDifficulty,
                    MaxEnemyStatMultiplier = authoring._maxEnemyStatMultiplier,
                    MaxSpawnRateMultiplier = authoring._maxSpawnRateMultiplier,
                    LevelStatBonus = authoring._levelStatBonus
                });
            }
        }
    }
}
