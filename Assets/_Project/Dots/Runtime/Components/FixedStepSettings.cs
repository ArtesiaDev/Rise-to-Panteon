using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct FixedStepSettings : IComponentData
    {
        public float Timestep;
        public bool IsSet;
    }
}
