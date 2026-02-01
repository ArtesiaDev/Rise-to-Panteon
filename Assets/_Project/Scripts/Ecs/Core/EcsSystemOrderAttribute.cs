using System;

namespace RuntimeRoguelike.Ecs
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EcsSystemOrderAttribute : Attribute
    {
        public int Order { get; }

        public EcsSystemOrderAttribute(int order)
        {
            Order = order;
        }
    }
}
