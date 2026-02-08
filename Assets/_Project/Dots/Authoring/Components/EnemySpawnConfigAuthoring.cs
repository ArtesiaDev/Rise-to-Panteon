using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class EnemySpawnConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int _initialCount = 12;
        [SerializeField] private int _maxCount = 40;
        [SerializeField] private float _spawnInterval = 6f;
        [SerializeField] private int _spawnAttempts = 200;

        private class Baker : Baker<EnemySpawnConfigAuthoring>
        {
            public override void Bake(EnemySpawnConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemySpawnConfigData
                {
                    InitialCount = authoring._initialCount,
                    MaxCount = authoring._maxCount,
                    SpawnInterval = authoring._spawnInterval,
                    SpawnAttempts = authoring._spawnAttempts
                });
            }
        }
    }
}
