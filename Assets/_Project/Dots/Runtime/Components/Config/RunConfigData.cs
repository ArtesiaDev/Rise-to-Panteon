using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    public struct RunConfigData : IComponentData
    {
        public bool RandomizeSeedOnStart;
        public int InitialSeed;
        public int2 MapSize;
    }
}
