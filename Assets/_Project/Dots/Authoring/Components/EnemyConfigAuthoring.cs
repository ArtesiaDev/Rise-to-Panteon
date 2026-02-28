using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class EnemyConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int _maxHealth = 4;
        [SerializeField] private int _baseDamage = 1;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _aggroRange = 8f;
        [SerializeField] private float _attackRange = 1.1f;
        [SerializeField] private float _attackCooldown = 1.2f;
        [SerializeField] private float _pathRefreshInterval = 0.6f;
        [SerializeField] private float _idleMoveInterval = 2.5f;

        private class Baker : Baker<EnemyConfigAuthoring>
        {
            public override void Bake(EnemyConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyConfigData
                {
                    MaxHealth = authoring._maxHealth,
                    BaseDamage = authoring._baseDamage,
                    MoveSpeed = authoring._moveSpeed,
                    AggroRange = authoring._aggroRange,
                    AttackRange = authoring._attackRange,
                    AttackCooldown = authoring._attackCooldown,
                    PathRefreshInterval = authoring._pathRefreshInterval,
                    IdleMoveInterval = authoring._idleMoveInterval
                });
            }
        }
    }
}
