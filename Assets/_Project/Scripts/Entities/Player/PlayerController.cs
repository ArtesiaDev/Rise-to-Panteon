using System;
using RuntimeRoguelike.Configs;
using UnityEngine;
using Zenject;

namespace RuntimeRoguelike
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public event Action<Vector2Int> OnCellChanged;

        private PlayerConfig _playerConfig;
        private GridPositionConverter _gridPositionConverter;

        private MapGrid _grid;
        private GridOccupancy _occupancy;
        private PlayerStats _stats;
        private Rigidbody2D _body;
        private Vector2Int _currentCell;
        private Vector2Int _targetCell;
        private Vector2 _targetPosition;
        private bool _isMoving;
        private Vector2Int _lastMoveDirection = Vector2Int.right;

        public Vector2Int CurrentCell => _currentCell;
        public Vector2Int LastMoveDirection => _lastMoveDirection;

        [Inject]
        private void Construct(PlayerConfig playerConfig, GridPositionConverter gridPositionConverter)
        {
            _playerConfig = playerConfig;
            _gridPositionConverter = gridPositionConverter;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (_grid == null || _occupancy == null || _isMoving)
            {
                return;
            }

            var direction = ReadInputDirection();
            if (direction == Vector2Int.zero)
            {
                return;
            }

            _lastMoveDirection = direction;
            var nextCell = _currentCell + direction;
            if (!_grid.IsWalkable(nextCell))
            {
                return;
            }

            if (!_occupancy.TryMove(_currentCell, nextCell))
            {
                return;
            }

            _targetCell = nextCell;
            _targetPosition = _gridPositionConverter.CellToWorld(_targetCell);
            _isMoving = true;
        }

        private void FixedUpdate()
        {
            if (!_isMoving)
            {
                return;
            }

            var speed = _playerConfig.MoveSpeed * (_stats != null ? _stats.MoveSpeedMultiplier : 1f);
            var nextPosition = Vector2.MoveTowards(_body.position, _targetPosition, speed * Time.fixedDeltaTime);
            _body.MovePosition(nextPosition);

            if (Vector2.Distance(nextPosition, _targetPosition) <= 0.01f)
            {
                _body.MovePosition(_targetPosition);
                _currentCell = _targetCell;
                _isMoving = false;
                OnCellChanged?.Invoke(_currentCell);
            }
        }

        public void Initialize(MapGrid mapGrid, GridOccupancy gridOccupancy, Vector2Int startCell, PlayerStats playerStats)
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody2D>();
            }

            _grid = mapGrid;
            _occupancy = gridOccupancy;
            _stats = playerStats;
            _currentCell = startCell;
            _targetCell = startCell;
            _targetPosition = _gridPositionConverter.CellToWorld(startCell);
            _body.position = _targetPosition;
            _occupancy.Occupy(startCell);
            OnCellChanged?.Invoke(startCell);
        }

        public void TeleportTo(Vector2Int cell)
        {
            if (_grid == null || _occupancy == null)
            {
                return;
            }

            if (!_grid.IsWalkable(cell))
            {
                return;
            }

            _occupancy.Release(_currentCell);
            _occupancy.Occupy(cell);
            _currentCell = cell;
            _targetCell = cell;
            _targetPosition = _gridPositionConverter.CellToWorld(cell);
            _body.position = _targetPosition;
            _isMoving = false;
            OnCellChanged?.Invoke(_currentCell);
        }

        private Vector2Int ReadInputDirection()
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
