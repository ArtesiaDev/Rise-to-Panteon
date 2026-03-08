using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    [CreateAssetMenu(menuName = "Config/Enemy")]
    public class EnemyConfigSO : ScriptableObject, IConfigApplier
    {
        public int MaxHealth = 4;
        public int BaseDamage = 1;
        public float MoveSpeed = 3f;
        public float AggroRange = 8f;
        public float AttackRange = 1.1f;
        public float AttackCooldown = 1.2f;
        public float PathRefreshInterval = 0.6f;
        public float IdleMoveInterval = 2.5f;

        public void Apply(Entity entity, EntityManager em)
        {
            em.AddComponentData(entity, new EnemyConfigData
            {
                MaxHealth = MaxHealth,
                BaseDamage = BaseDamage,
                MoveSpeed = MoveSpeed,
                AggroRange = AggroRange,
                AttackRange = AttackRange,
                AttackCooldown = AttackCooldown,
                PathRefreshInterval = PathRefreshInterval,
                IdleMoveInterval = IdleMoveInterval
            });
        }
    }
}
