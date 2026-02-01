using System;
using UnityEngine;

namespace RuntimeRoguelike
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;

        private MapGrid grid;
        private GridOccupancy occupancy;
        private PlayerStats stats;
        private Rigidbody2D body;
        private Vector2Int currentCell;
        private Vector2Int targetCell;
        private Vector2 targetPosition;
        private bool isMoving;
        private Vector2Int lastMoveDirection = Vector2Int.right;

        public Vector2Int CurrentCell => currentCell;
        public Vector2Int LastMoveDirection => lastMoveDirection;

        public event Action<Vector2Int> OnCellChanged;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (grid == null || occupancy == null || isMoving)
            {
                return;
            }

            var direction = ReadInputDirection();
            if (direction == Vector2Int.zero)
            {
                return;
            }

            lastMoveDirection = direction;
            var nextCell = currentCell + direction;
            if (!grid.IsWalkable(nextCell))
            {
                return;
            }

            if (!occupancy.TryMove(currentCell, nextCell))
            {
                return;
            }

            targetCell = nextCell;
            targetPosition = GridUtils.CellToWorld(targetCell);
            isMoving = true;
        }

        private void FixedUpdate()
        {
            if (!isMoving)
            {
                return;
            }

            var speed = moveSpeed * (stats != null ? stats.MoveSpeedMultiplier : 1f);
            var nextPosition = Vector2.MoveTowards(body.position, targetPosition, speed * Time.fixedDeltaTime);
            body.MovePosition(nextPosition);

            if (Vector2.Distance(nextPosition, targetPosition) <= 0.01f)
            {
                body.MovePosition(targetPosition);
                currentCell = targetCell;
                isMoving = false;
                OnCellChanged?.Invoke(currentCell);
            }
        }

        public void Initialize(MapGrid mapGrid, GridOccupancy gridOccupancy, Vector2Int startCell, PlayerStats playerStats)
        {
            grid = mapGrid;
            occupancy = gridOccupancy;
            stats = playerStats;
            currentCell = startCell;
            targetCell = startCell;
            targetPosition = GridUtils.CellToWorld(startCell);
            body.position = targetPosition;
            occupancy.Occupy(startCell);
            OnCellChanged?.Invoke(startCell);
        }

        public void TeleportTo(Vector2Int cell)
        {
            if (grid == null || occupancy == null)
            {
                return;
            }

            if (!grid.IsWalkable(cell))
            {
                return;
            }

            occupancy.Release(currentCell);
            occupancy.Occupy(cell);
            currentCell = cell;
            targetCell = cell;
            targetPosition = GridUtils.CellToWorld(cell);
            body.position = targetPosition;
            isMoving = false;
            OnCellChanged?.Invoke(currentCell);
        }

        private static Vector2Int ReadInputDirection()
        {
            var x = 0;
            var y = 0;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                x -= 1;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                x += 1;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                y += 1;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                y -= 1;
            }

            if (x != 0)
            {
                return new Vector2Int(x, 0);
            }

            if (y != 0)
            {
                return new Vector2Int(0, y);
            }

            return Vector2Int.zero;
        }
    }
}
