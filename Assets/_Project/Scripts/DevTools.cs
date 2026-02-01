using UnityEngine;

namespace RuntimeRoguelike
{
    public class DevTools : MonoBehaviour
    {
        [SerializeField] private bool enabledTools = true;
        [SerializeField] private KeyCode restartKey = KeyCode.F5;
        [SerializeField] private KeyCode toggleGizmosKey = KeyCode.G;
        [SerializeField] private KeyCode teleportKey = KeyCode.T;

        private RunController runController;
        private PlayerController player;
        private MapGrid grid;
        private GridOccupancy occupancy;
        private RunStats runStats;
        private bool showGizmos;

        public void Initialize(RunController controller, PlayerController playerController, MapGrid mapGrid, GridOccupancy gridOccupancy, RunStats stats)
        {
            runController = controller;
            player = playerController;
            grid = mapGrid;
            occupancy = gridOccupancy;
            runStats = stats;
        }

        private void Update()
        {
            if (!enabledTools)
            {
                return;
            }

            if (Input.GetKeyDown(restartKey))
            {
                runController.RestartRun();
            }

            if (Input.GetKeyDown(toggleGizmosKey))
            {
                showGizmos = !showGizmos;
            }

            if (Input.GetKeyDown(teleportKey))
            {
                TeleportToMouseCell();
            }
        }

        private void OnGUI()
        {
            if (!enabledTools || runStats == null)
            {
                return;
            }

            GUI.Label(new Rect(10f, 160f, 240f, 20f), $"Seed: {runStats.Seed}");
            GUI.Label(new Rect(10f, 180f, 240f, 20f), $"Player Cell: {runStats.PlayerCell.x}, {runStats.PlayerCell.y}");
            GUI.Label(new Rect(10f, 200f, 240f, 20f), $"Enemies: {runStats.EnemyCount}");
            GUI.Label(new Rect(10f, 220f, 240f, 20f), $"Loot: {runStats.LootCount}");
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || player == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(GridUtils.CellToWorld(player.CurrentCell), Vector3.one);
        }

        private void TeleportToMouseCell()
        {
            if (player == null || grid == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var world = camera.ScreenToWorldPoint(Input.mousePosition);
            var cell = GridUtils.WorldToCell(world);
            if (!grid.IsWalkable(cell))
            {
                return;
            }

            if (occupancy != null && occupancy.IsOccupied(cell))
            {
                return;
            }

            player.TeleportTo(cell);
        }
    }
}
