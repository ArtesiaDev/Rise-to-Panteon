using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;

namespace RuntimeRoguelike.Dots.Hybrid
{
    /// <summary>
    /// Releases GO/pool for destroyed entities by delegating to SpriteRenderBridge.
    /// Runs in PresentationSystemGroup after RenderInterpolationSystem.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(RenderInterpolationSystem))]
    public partial struct PresentationCleanupSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (SpriteRenderBridge.Instance != null)
                SpriteRenderBridge.Instance.CleanupDestroyedViews();
        }
    }
}
