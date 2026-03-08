using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    /// <summary>
    /// Состояние миникарты (вкл/выкл). Toggle через InputBridge.
    /// </summary>
    public struct MinimapToggleTag : IComponentData, IEnableableComponent
    {
    }
}
