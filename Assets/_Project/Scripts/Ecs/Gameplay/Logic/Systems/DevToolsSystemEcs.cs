using RuntimeRoguelike.Configs;
using UnityEngine;

namespace RuntimeRoguelike.Ecs
{
    public class DevToolsSystemEcs : IEcsUpdateSystem
    {
        private readonly DevToolsConfig _config;
        private readonly IInputService _inputService;
        private readonly GridPositionConverter _gridPositionConverter;

        public DevToolsSystemEcs(DevToolsConfig config, IInputService inputService, GridPositionConverter gridPositionConverter)
        {
            _config = config;
            _inputService = inputService;
            _gridPositionConverter = gridPositionConverter;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!_config.Enabled)
            {
                return;
            }

            var input = _inputService.Read();
            if (input.ToggleGizmosPressed && world.TryGetResource<DebugState>(out var debug))
            {
                debug.ShowGizmos = !debug.ShowGizmos;
            }

            if (input.TeleportPressed)
            {
                TeleportPlayer(world);
            }
        }

        private void TeleportPlayer(EcsWorld world)
        {
            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            if (!world.TryGetResource<MapGrid>(out var mapGrid))
            {
                return;
            }

            if (!world.TryGetResource<GridOccupancy>(out var occupancy))
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var worldPos = camera.ScreenToWorldPoint(Input.mousePosition);
            var cell = _gridPositionConverter.WorldToCell(worldPos);
            var target = new Int2(cell.x, cell.y);
            if (!mapGrid.IsWalkable(target))
            {
                return;
            }

            if (occupancy.IsOccupied(target))
            {
                return;
            }

            var playerId = counters.PlayerEntityId;
            var positionPool = world.GetPool<GridPosition>();
            if (!positionPool.Has(playerId))
            {
                return;
            }

            var from = positionPool.GetRef(playerId).Value;
            occupancy.Release(from);
            occupancy.Occupy(target);
            positionPool.GetRef(playerId).Value = target;
            counters.PlayerCell = target;

            var renderPool = world.GetPool<RenderPosition>();
            if (renderPool.Has(playerId))
            {
                renderPool.GetRef(playerId).Value = new Float2(target.X, target.Y);
            }
        }
    }
}
