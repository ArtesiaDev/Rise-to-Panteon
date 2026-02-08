using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class PlayerConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 12;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private Vector2 colliderSize = new Vector2(0.8f, 0.8f);

        private class Baker : Baker<PlayerConfigAuthoring>
        {
            public override void Bake(PlayerConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayerConfigData
                {
                    MaxHealth = authoring.maxHealth,
                    MoveSpeed = authoring.moveSpeed,
                    ColliderSize = new float2(authoring.colliderSize.x, authoring.colliderSize.y)
                });
            }
        }
    }
}
