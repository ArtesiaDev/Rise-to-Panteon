using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    [CreateAssetMenu(menuName = "Config/EnemySpawn")]
    public class EnemySpawnConfigSO : ScriptableObject, IConfigApplier
    {
        public int InitialCount = 12;
        public int MaxCount = 40;
        public float SpawnInterval = 6f;
        public int SpawnAttempts = 200;

        public void Apply(Entity entity, EntityManager em)
        {
            em.AddComponentData(entity, new EnemySpawnConfigData
            {
                InitialCount = InitialCount,
                MaxCount = MaxCount,
                SpawnInterval = SpawnInterval,
                SpawnAttempts = SpawnAttempts
            });
        }
    }
}
