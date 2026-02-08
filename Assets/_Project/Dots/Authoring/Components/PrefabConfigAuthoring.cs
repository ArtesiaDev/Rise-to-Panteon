using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class PrefabConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject lootPrefab;

        private class Baker : Baker<PrefabConfigAuthoring>
        {
            public override void Bake(PrefabConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PrefabConfigData
                {
                    Player = authoring.playerPrefab != null
                        ? GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null,
                    Enemy = authoring.enemyPrefab != null
                        ? GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null,
                    Loot = authoring.lootPrefab != null
                        ? GetEntity(authoring.lootPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null
                });
            }
        }
    }
}
