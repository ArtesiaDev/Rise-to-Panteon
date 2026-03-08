using RuntimeRoguelike.Dots.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class InputBridge : MonoBehaviour
    {
        [SerializeField] private KeyCode _attackKey = KeyCode.Space;
        [SerializeField] private KeyCode _restartKey = KeyCode.R;
        [SerializeField] private KeyCode _toggleGizmosKey = KeyCode.G;
        [SerializeField] private KeyCode _teleportKey = KeyCode.T;
        [SerializeField] private KeyCode _minimapToggleKey = KeyCode.Tab;

        private EntityManager _entityManager;
        private EntityQuery _inputQuery;
        private World _world;

        private void Awake()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = _world.EntityManager;
            _inputQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<InputState>());
        }

        private void Update()
        {
            if (_world is not { IsCreated: true })
                return;

            var move = int2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                move = new int2(0, 1);
            }
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                move = new int2(0, -1);
            }
            else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                move = new int2(-1, 0);
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                move = new int2(1, 0);
            }

            var attackPressed = Input.GetKeyDown(_attackKey);
            var restartPressed = Input.GetKeyDown(_restartKey);
            var toggleGizmos = Input.GetKeyDown(_toggleGizmosKey);
            var teleportPressed = Input.GetKeyDown(_teleportKey);
            var minimapToggle = Input.GetKeyDown(_minimapToggleKey);

            // Toggle миникарты через MinimapToggleTag
            if (minimapToggle)
            {
                ToggleMinimap();
            }

            // Для телепорта ищем случайную свободную клетку
            var teleportRequested = false;
            var teleportTarget = int2.zero;
            if (teleportPressed)
            {
                teleportTarget = FindRandomFreeCell();
                teleportRequested = teleportTarget.x >= 0;
            }

            // InputState создаётся в RunBootstrapSystem.OnCreate — ждём пока появится
            if (_inputQuery.IsEmpty)
                return;

            var inputEntity = _inputQuery.GetSingletonEntity();
            var input = _entityManager.GetComponentData<InputState>(inputEntity);
            input.MoveDir = move;
            input.AttackPressed |= attackPressed;
            input.RestartPressed |= restartPressed;
            input.ToggleGizmos |= toggleGizmos;
            if (teleportRequested)
            {
                input.TeleportRequested = true;
                input.TeleportTarget = teleportTarget;
            }
            _entityManager.SetComponentData(inputEntity, input);
        }

        /// <summary>
        /// Toggle состояния миникарты (MinimapToggleTag enable/disable).
        /// </summary>
        private void ToggleMinimap()
        {
            var runQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RunState>());
            if (runQuery.IsEmpty)
                return;

            var runEntity = runQuery.GetSingletonEntity();
            if (!_entityManager.HasComponent<MinimapToggleTag>(runEntity))
                return;

            var isEnabled = _entityManager.IsComponentEnabled<MinimapToggleTag>(runEntity);
            _entityManager.SetComponentEnabled<MinimapToggleTag>(runEntity, !isEnabled);
        }

        /// <summary>
        /// Ищет случайную свободную (floor + нет occupancy) клетку на карте.
        /// Возвращает (-1,-1) если не удалось найти.
        /// </summary>
        private int2 FindRandomFreeCell()
        {
            var runQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RunState>());
            if (runQuery.IsEmpty)
                return new int2(-1, -1);

            var runEntity = runQuery.GetSingletonEntity();
            if (!_entityManager.HasComponent<MapBlobReference>(runEntity))
                return new int2(-1, -1);

            var mapRef = _entityManager.GetComponentData<MapBlobReference>(runEntity);
            if (!mapRef.Value.IsCreated)
                return new int2(-1, -1);

            ref var map = ref mapRef.Value.Value;
            var occupancy = _entityManager.GetBuffer<CellOccupant>(runEntity);

            var rng = Random.CreateFromIndex((uint)(Time.frameCount + 7919));
            const int maxAttempts = 100;

            for (var i = 0; i < maxAttempts; i++)
            {
                var cell = new int2(
                    rng.NextInt(1, map.Size.x - 1),
                    rng.NextInt(1, map.Size.y - 1));

                if (!MapUtilities.IsWalkable(ref map, cell))
                    continue;

                var index = MapUtilities.ToIndex(cell, map.Size);
                if (occupancy[index].Value != Entity.Null)
                    continue;

                return cell;
            }

            return new int2(-1, -1);
        }
    }
}
