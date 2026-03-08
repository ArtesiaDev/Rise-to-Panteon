using Unity.Entities;

namespace RuntimeRoguelike.Dots.Hybrid
{
    /// <summary>
    /// Интерфейс для ScriptableObject-конфигов, умеющих записывать себя в ECS.
    /// </summary>
    public interface IConfigApplier
    {
        void Apply(Entity entity, EntityManager em);
    }
}
