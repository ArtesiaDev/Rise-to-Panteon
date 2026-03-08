using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct RunConfigData : IComponentData
    {
        public bool RandomizeSeedOnStart;
        public int InitialSeed;
    }
}
