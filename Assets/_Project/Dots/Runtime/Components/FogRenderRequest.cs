using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    /// <summary>
    /// Триггер обновления fog-рендера. Активируется FogOfWarSystem при изменении видимости.
    /// </summary>
    public struct FogRenderRequest : IComponentData, IEnableableComponent
    {
    }
}
