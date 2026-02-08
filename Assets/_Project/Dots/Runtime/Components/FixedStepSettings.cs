using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct FixedStepSettings : IComponentData
    {
        public float Timestep;
        public bool IsSet;
    }
}
