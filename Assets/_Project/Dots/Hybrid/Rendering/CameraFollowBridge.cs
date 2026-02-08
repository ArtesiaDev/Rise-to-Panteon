using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class CameraFollowBridge : MonoBehaviour
    {
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Vector3 _offset = new(0f, 0f, -10f);

        private EntityManager _entityManager;
        private EntityQuery _playerQuery;

        private void Awake()
        {
            if (_targetCamera == null)
                _targetCamera = Camera.main;

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

        private void LateUpdate()
        {
            if (_targetCamera == null || _playerQuery.IsEmpty) return;
            
            var playerEntity = _playerQuery.GetSingletonEntity();
            var position = _entityManager.GetComponentData<RenderPosition>(playerEntity).Value;
            _targetCamera.transform.position = new Vector3(position.x, position.y, 0f) + _offset;
        }
    }
}
