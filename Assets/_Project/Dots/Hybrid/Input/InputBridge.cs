using RuntimeRoguelike.Dots.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = Unity.Mathematics.Random;

namespace RuntimeRoguelike.Dots.Hybrid
{
    /// <summary>
    /// Читает project-wide Input Actions (Assets/Settings/InputSystem_Actions, карта Player)
    /// и пишет их в singleton InputState. Биндинги настраиваются в ассете, не в коде.
    /// </summary>
    public class InputBridge : MonoBehaviour
    {
        // Порог отклонения стика, после которого направление считается нажатым
        private const float MoveThreshold = 0.5f;

        private EntityManager _entityManager;
        private EntityQuery _inputQuery;
        private World _world;

        private InputAction _move;
        private InputAction _attack;
        private InputAction _restart;
        private InputAction _toggleGizmos;
        private InputAction _teleport;
        private InputAction _toggleMinimap;

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

            // Project-wide actions включены Input System автоматически
            var actions = InputSystem.actions;
            _move = actions.FindAction("Player/Move", throwIfNotFound: true);
            _attack = actions.FindAction("Player/Attack", throwIfNotFound: true);
            _restart = actions.FindAction("Player/Restart", throwIfNotFound: true);
            _toggleGizmos = actions.FindAction("Player/ToggleGizmos", throwIfNotFound: true);
            _teleport = actions.FindAction("Player/Teleport", throwIfNotFound: true);
            _toggleMinimap = actions.FindAction("Player/ToggleMinimap", throwIfNotFound: true);
        }

        private void Update()
        {
            if (_world is not { IsCreated: true })
                return;

            var move = ToGridDirection(_move.ReadValue<Vector2>());

            var attackPressed = _attack.WasPressedThisFrame();
            var restartPressed = _restart.WasPressedThisFrame();
            var toggleGizmos = _toggleGizmos.WasPressedThisFrame();
            var teleportPressed = _teleport.WasPressedThisFrame();
            var minimapToggle = _toggleMinimap.WasPressedThisFrame();

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
        /// Переводит вектор Move в одно из 4 направлений сетки.
        /// Вертикаль в приоритете (как было с W/S над A/D), при равенстве осей — тоже вертикаль.
        /// </summary>
        private static int2 ToGridDirection(Vector2 value)
        {
            var absX = math.abs(value.x);
            var absY = math.abs(value.y);

            if (absY >= absX && absY > MoveThreshold)
                return new int2(0, value.y > 0 ? 1 : -1);

            if (absX > MoveThreshold)
                return new int2(value.x > 0 ? 1 : -1, 0);

            return int2.zero;
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
