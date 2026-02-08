using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class RunConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private bool _randomizeSeedOnStart = true;
        [SerializeField] private int _initialSeed;
        [SerializeField] private Vector2Int _mapSize = new(200, 200);

        private class Baker : Baker<RunConfigAuthoring>
        {
            public override void Bake(RunConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new RunConfigData
                {
                    RandomizeSeedOnStart = authoring._randomizeSeedOnStart,
                    InitialSeed = authoring._initialSeed,
                    MapSize = new int2(authoring._mapSize.x, authoring._mapSize.y)
                });
            }
        }
    }
}
