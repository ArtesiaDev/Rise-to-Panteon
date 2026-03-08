using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class RunConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private bool _randomizeSeedOnStart = true;
        [SerializeField] private int _initialSeed;

        private class Baker : Baker<RunConfigAuthoring>
        {
            public override void Bake(RunConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new RunConfigData
                {
                    RandomizeSeedOnStart = authoring._randomizeSeedOnStart,
                    InitialSeed = authoring._initialSeed
                });
            }
        }
    }
}
