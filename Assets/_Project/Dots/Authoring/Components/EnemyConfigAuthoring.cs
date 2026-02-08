using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class EnemyConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 4;
        [SerializeField] private int baseDamage = 1;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float aggroRange = 8f;
        [SerializeField] private float attackRange = 1.1f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float pathRefreshInterval = 0.6f;
        [SerializeField] private float idleMoveInterval = 2.5f;
        [SerializeField] private Vector2 colliderSize = new Vector2(0.8f, 0.8f);

        private class Baker : Baker<EnemyConfigAuthoring>
        {
            public override void Bake(EnemyConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyConfigData
                {
                    MaxHealth = authoring.maxHealth,
                    BaseDamage = authoring.baseDamage,
                    MoveSpeed = authoring.moveSpeed,
                    AggroRange = authoring.aggroRange,
                    AttackRange = authoring.attackRange,
                    AttackCooldown = authoring.attackCooldown,
                    PathRefreshInterval = authoring.pathRefreshInterval,
                    IdleMoveInterval = authoring.idleMoveInterval,
                    ColliderSize = new float2(authoring.colliderSize.x, authoring.colliderSize.y)
                });
            }
        }
    }
}
