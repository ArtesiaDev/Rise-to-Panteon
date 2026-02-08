using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class PlayerAttackConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int baseDamage = 2;
        [SerializeField] private float attackCooldown = 0.4f;
        [SerializeField] private float attackRange = 0.7f;
        [SerializeField] private float attackOffset = 0.6f;

        private class Baker : Baker<PlayerAttackConfigAuthoring>
        {
            public override void Bake(PlayerAttackConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayerAttackConfigData
                {
                    BaseDamage = authoring.baseDamage,
                    AttackCooldown = authoring.attackCooldown,
                    AttackRange = authoring.attackRange,
                    AttackOffset = authoring.attackOffset
                });
            }
        }
    }
}
