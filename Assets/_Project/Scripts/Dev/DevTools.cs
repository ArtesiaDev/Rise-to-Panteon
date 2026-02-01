using RuntimeRoguelike.Configs;
using UnityEngine;
using Zenject;

namespace RuntimeRoguelike
{
    public class DevTools : MonoBehaviour
    {
        private DevToolsConfig _config;
        private GridPositionConverter _gridPositionConverter;

        private RunController _runController;
        private PlayerController _player;
        private MapGrid _grid;
        private GridOccupancy _occupancy;
        private RunStats _runStats;
        private bool _showGizmos;

        [Inject]
        private void Construct(DevToolsConfig config, GridPositionConverter gridPositionConverter)
        {
            _config = config;
            _gridPositionConverter = gridPositionConverter;
        }

        private void Update()
        {
            if (!_config.Enabled)
            {
                return;
            }

            if (Input.GetKeyDown(_config.RestartKey))
            {
                _runController.RestartRun();
            }

            if (Input.GetKeyDown(_config.ToggleGizmosKey))
            {
                _showGizmos = !_showGizmos;
            }

            if (Input.GetKeyDown(_config.TeleportKey))
            {
                TeleportToMouseCell();
            }
        }

        private void OnGUI()
        {
            if (!_config.Enabled || _runStats == null)
            {
                return;
            }

            GUI.Label(new Rect(10f, 160f, 240f, 20f), $"Seed: {_runStats.Seed}");
            GUI.Label(new Rect(10f, 180f, 240f, 20f), $"Player Cell: {_runStats.PlayerCell.x}, {_runStats.PlayerCell.y}");
            GUI.Label(new Rect(10f, 200f, 240f, 20f), $"Enemies: {_runStats.EnemyCount}");
            GUI.Label(new Rect(10f, 220f, 240f, 20f), $"Loot: {_runStats.LootCount}");
        }

        private void OnDrawGizmos()
        {
            if (!_showGizmos || _player == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_gridPositionConverter.CellToWorld(_player.CurrentCell), Vector3.one);
        }

        public void Initialize(RunController controller, PlayerController playerController, MapGrid mapGrid, GridOccupancy gridOccupancy, RunStats stats)
        {
            _runController = controller;
            _player = playerController;
            _grid = mapGrid;
            _occupancy = gridOccupancy;
            _runStats = stats;
        }

        private void TeleportToMouseCell()
        {
            if (_player == null || _grid == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var world = camera.ScreenToWorldPoint(Input.mousePosition);
            var cell = _gridPositionConverter.WorldToCell(world);
            if (!_grid.IsWalkable(cell))
            {
                return;
            }

            if (_occupancy != null && _occupancy.IsOccupied(cell))
            {
                return;
            }

            _player.TeleportTo(cell);
        }
    }
}
