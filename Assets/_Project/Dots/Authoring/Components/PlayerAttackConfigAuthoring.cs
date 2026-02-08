using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class PlayerAttackConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int _baseDamage = 2;
        [SerializeField] private float _attackCooldown = 0.4f;
        [SerializeField] private float _attackRange = 0.7f;
        [SerializeField] private float _attackOffset = 0.6f;

        private class Baker : Baker<PlayerAttackConfigAuthoring>
        {
            public override void Bake(PlayerAttackConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayerAttackConfigData
                {
                    BaseDamage = authoring._baseDamage,
                    AttackCooldown = authoring._attackCooldown,
                    AttackRange = authoring._attackRange,
                    AttackOffset = authoring._attackOffset
                });
            }
        }
    }
}
