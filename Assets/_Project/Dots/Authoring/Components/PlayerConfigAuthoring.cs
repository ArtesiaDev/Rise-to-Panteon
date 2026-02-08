using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class PlayerConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int _maxHealth = 12;
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private Vector2 _colliderSize = new(0.8f, 0.8f);

        private class Baker : Baker<PlayerConfigAuthoring>
        {
            public override void Bake(PlayerConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayerConfigData
                {
                    MaxHealth = authoring._maxHealth,
                    MoveSpeed = authoring._moveSpeed,
                    ColliderSize = new float2(authoring._colliderSize.x, authoring._colliderSize.y)
                });
            }
        }
    }
}
