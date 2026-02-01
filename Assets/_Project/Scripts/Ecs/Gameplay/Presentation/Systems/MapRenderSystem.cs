using UnityEngine;

namespace RuntimeRoguelike.Ecs
{
    public class MapRenderSystem : IEcsUpdateSystem
    {
        private readonly TilemapWorldRenderer _renderer;
        private readonly WorldRoots _roots;

        public MapRenderSystem(TilemapWorldRenderer renderer, WorldRoots roots)
        {
            _renderer = renderer;
            _roots = roots;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<MapRenderRequest>(out var request) || !request.Pending)
            {
                return;
            }

            if (!world.TryGetResource<MapGrid>(out var mapGrid))
            {
                return;
            }

            request.Pending = false;
            if (_roots.WorldRoot != null)
            {
                for (var i = _roots.WorldRoot.childCount - 1; i >= 0; i--)
                {
                    Object.Destroy(_roots.WorldRoot.GetChild(i).gameObject);
                }
            }

            _renderer.Render(mapGrid, _roots.WorldRoot);
        }
    }
}
