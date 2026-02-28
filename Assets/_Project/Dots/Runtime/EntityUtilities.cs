using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    /// <summary>
    /// Общие утилиты для работы с entity-компонентами.
    /// </summary>
    public static class EntityUtilities
    {
        /// <summary>
        /// Добавляет компонент если его нет, иначе обновляет значение.
        /// </summary>
        public static void EnsureComponent<T>(EntityManager em, Entity e, T data)
            where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(e))
            {
                em.SetComponentData(e, data);
            }
            else
            {
                em.AddComponentData(e, data);
            }
        }
    }
}
