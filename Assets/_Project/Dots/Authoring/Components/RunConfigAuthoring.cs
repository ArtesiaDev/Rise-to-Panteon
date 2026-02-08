using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class RunConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private bool randomizeSeedOnStart = true;
        [SerializeField] private int initialSeed;
        [SerializeField] private Vector2Int mapSize = new Vector2Int(200, 200);

        private class Baker : Baker<RunConfigAuthoring>
        {
            public override void Bake(RunConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new RunConfigData
                {
                    RandomizeSeedOnStart = authoring.randomizeSeedOnStart,
                    InitialSeed = authoring.initialSeed,
                    MapSize = new int2(authoring.mapSize.x, authoring.mapSize.y)
                });
            }
        }
    }
}
