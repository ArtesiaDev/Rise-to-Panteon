using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class GizmosBridge : MonoBehaviour
    {
        [SerializeField] private bool _drawOccupancy = true;
        [SerializeField] private Color _occupancyColor = new(0.2f, 0.6f, 1f, 0.4f);

        private EntityManager _entityManager;
        private EntityQuery _runQuery;
        private EntityQuery _inputQuery;
        private bool _enabledState;

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = world.EntityManager;
            _runQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RunState>(), ComponentType.ReadOnly<MapBlobReference>());
            _inputQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<InputState>());
        }

        private void Update()
        {
            if (_inputQuery.IsEmpty)
            {
                return;
            }

            var input = _inputQuery.GetSingleton<InputState>();
            if (input.ToggleGizmos)
            {
                _enabledState = !_enabledState;
                _entityManager.SetComponentData(_inputQuery.GetSingletonEntity(), new InputState
                {
                    MoveDir = input.MoveDir,
                    AttackPressed = input.AttackPressed,
                    RestartPressed = input.RestartPressed,
                    ToggleGizmos = false,
                    TeleportRequested = input.TeleportRequested,
                    TeleportTarget = input.TeleportTarget
                });
            }
        }

        private void OnDrawGizmos()
        {
            if (!_enabledState || !_drawOccupancy || _runQuery.IsEmpty)
            {
                return;
            }

            var mapRef = _runQuery.GetSingleton<MapBlobReference>();
            if (!mapRef.Value.IsCreated)
            {
                return;
            }

            ref var map = ref mapRef.Value.Value;
            var runEntity = _runQuery.GetSingletonEntity();
            if (!_entityManager.HasBuffer<CellOccupant>(runEntity))
            {
                return;
            }

            var occupancy = _entityManager.GetBuffer<CellOccupant>(runEntity);
            Gizmos.color = _occupancyColor;

            for (var y = 0; y < map.Size.y; y++)
            {
                for (var x = 0; x < map.Size.x; x++)
                {
                    var index = y * map.Size.x + x;
                    if (occupancy[index].Value == Entity.Null)
                    {
                        continue;
                    }

                    var center = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    Gizmos.DrawCube(center, Vector3.one * 0.9f);
                }
            }
        }
    }
}
