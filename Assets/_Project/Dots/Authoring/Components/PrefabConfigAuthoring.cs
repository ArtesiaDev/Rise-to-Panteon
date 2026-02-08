using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class PrefabConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private GameObject _lootPrefab;

        private class Baker : Baker<PrefabConfigAuthoring>
        {
            public override void Bake(PrefabConfigAuthoring authoring)
            {
                if (authoring._playerPrefab != null) DependsOn(authoring._playerPrefab);
                if (authoring._enemyPrefab != null) DependsOn(authoring._enemyPrefab);
                if (authoring._lootPrefab != null) DependsOn(authoring._lootPrefab);

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PrefabConfigData
                {
                    Player = authoring._playerPrefab != null
                        ? GetEntity(authoring._playerPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null,
                    Enemy = authoring._enemyPrefab != null
                        ? GetEntity(authoring._enemyPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null,
                    Loot = authoring._lootPrefab != null
                        ? GetEntity(authoring._lootPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null
                });
            }
        }
    }
}
