using RuntimeRoguelike.Configs;
using UnityEngine;

namespace RuntimeRoguelike.Ecs
{
    public class CameraFollowSystem : IEcsLateUpdateSystem
    {
        private readonly CameraFollowConfig _config;
        private readonly EntityViewRegistry _registry;
        private Camera _camera;
        private Vector3 _velocity;

        public CameraFollowSystem(CameraFollowConfig config, EntityViewRegistry registry)
        {
            _config = config;
            _registry = registry;
        }

        public void LateUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                return;
            }

            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            if (!_registry.TryGet(counters.PlayerEntityId, out var view))
            {
                return;
            }

            var targetPosition = new Vector3(view.Transform.position.x, view.Transform.position.y, _camera.transform.position.z);
            _camera.transform.position = Vector3.SmoothDamp(_camera.transform.position, targetPosition, ref _velocity, _config.SmoothTime);
        }
    }
}
