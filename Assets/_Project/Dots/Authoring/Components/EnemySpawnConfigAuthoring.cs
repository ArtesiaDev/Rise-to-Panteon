using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class EnemySpawnConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int initialCount = 12;
        [SerializeField] private int maxCount = 40;
        [SerializeField] private float spawnInterval = 6f;
        [SerializeField] private int spawnAttempts = 200;

        private class Baker : Baker<EnemySpawnConfigAuthoring>
        {
            public override void Bake(EnemySpawnConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemySpawnConfigData
                {
                    InitialCount = authoring.initialCount,
                    MaxCount = authoring.maxCount,
                    SpawnInterval = authoring.spawnInterval,
                    SpawnAttempts = authoring.spawnAttempts
                });
            }
        }
    }
}
