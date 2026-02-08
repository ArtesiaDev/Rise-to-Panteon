using RuntimeRoguelike.Dots;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class CameraFollowBridge : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        private EntityManager _entityManager;
        private EntityQuery _playerQuery;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = world.EntityManager;
            _playerQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<RenderPosition>());
        }

        private void OnDestroy()
        {
            if (_playerQuery.IsCreated)
            {
                _playerQuery.Dispose();
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null || _playerQuery.IsEmpty)
            {
                return;
            }

            var playerEntity = _playerQuery.GetSingletonEntity();
            var position = _entityManager.GetComponentData<RenderPosition>(playerEntity).Value;
            targetCamera.transform.position = new Vector3(position.x, position.y, 0f) + offset;
        }
    }
}
