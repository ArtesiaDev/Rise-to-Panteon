using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    [CreateAssetMenu(menuName = "Config/Player")]
    public class PlayerConfigSO : ScriptableObject, IConfigApplier
    {
        [Header("Базовые характеристики")]
        public int MaxHealth = 12;
        public float MoveSpeed = 4f;

        [Header("Атака")]
        public int BaseDamage = 2;
        public float AttackCooldown = 0.4f;
        public float AttackRange = 0.7f;
        public float AttackOffset = 0.6f;

        public void Apply(Entity entity, EntityManager em)
        {
            em.AddComponentData(entity, new PlayerConfigData
            {
                MaxHealth = MaxHealth,
                MoveSpeed = MoveSpeed
            });

            em.AddComponentData(entity, new PlayerAttackConfigData
            {
                BaseDamage = BaseDamage,
                AttackCooldown = AttackCooldown,
                AttackRange = AttackRange,
                AttackOffset = AttackOffset
            });
        }
    }
}
