using System.Collections.Generic;
using RuntimeRoguelike.Configs;
using UnityEngine;
using Zenject;

namespace RuntimeRoguelike
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        private EnemyConfig _enemyConfig;
        private GridPositionConverter _gridPositionConverter;

        private MapGrid _grid;
        private GridOccupancy _occupancy;
        private AStarPathfinder _pathfinder;
        private PlayerController _target;
        private Health _targetHealth;
        private Health _health;
        private System.Random _rng;

        private Rigidbody2D _body;
        private Vector2Int _currentCell;
        private Vector2Int _targetCell;
        private Vector2 _targetPosition;
        private bool _isMoving;
        private float _nextPathTime;
        private float _nextAttackTime;
        private float _nextIdleMoveTime;
        private List<Vector2Int> _currentPath = new List<Vector2Int>();
        private int _damageBonus;

        [Inject]
        private void Construct(EnemyConfig enemyConfig, GridPositionConverter gridPositionConverter)
        {
            _enemyConfig = enemyConfig;
            _gridPositionConverter = gridPositionConverter;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
        }

        private void Update()
        {
            if (_grid == null || _occupancy == null || _target == null)
            {
                return;
            }

            var distanceToPlayer = Mathf.Abs(_target.CurrentCell.x - _currentCell.x) + Mathf.Abs(_target.CurrentCell.y - _currentCell.y);
            if (distanceToPlayer <= _enemyConfig.AggroRange)
            {
                if (Time.time >= _nextPathTime)
                {
                    _currentPath = _pathfinder.FindPath(_currentCell, _target.CurrentCell);
                    _nextPathTime = Time.time + _enemyConfig.PathRefreshInterval;
                }

                TryAttack(distanceToPlayer);

                if (!_isMoving && _currentPath.Count > 0)
                {
                    TryMove(_currentPath[0]);
                }
            }
            else if (Time.time >= _nextIdleMoveTime && !_isMoving)
            {
                IdleStep();
                _nextIdleMoveTime = Time.time + _enemyConfig.IdleMoveInterval;
            }
        }

        private void FixedUpdate()
        {
            if (!_isMoving)
            {
                return;
            }

            var nextPosition = Vector2.MoveTowards(_body.position, _targetPosition, _enemyConfig.MoveSpeed * Time.fixedDeltaTime);
            _body.MovePosition(nextPosition);

            if (Vector2.Distance(nextPosition, _targetPosition) <= 0.01f)
            {
                _body.MovePosition(_targetPosition);
                _currentCell = _targetCell;
                _isMoving = false;
            }
        }

        public void Initialize(MapGrid mapGrid, GridOccupancy gridOccupancy, AStarPathfinder pathfinder, PlayerController player, Vector2Int startCell, int seed, int damageBonus)
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody2D>();
            }

            if (_health == null)
            {
                _health = GetComponent<Health>();
            }

            _grid = mapGrid;
            _occupancy = gridOccupancy;
            _pathfinder = pathfinder;
            _target = player;
            _targetHealth = player.GetComponent<Health>();
            _currentCell = startCell;
            _targetCell = startCell;
            _targetPosition = _gridPositionConverter.CellToWorld(startCell);
            _body.position = _targetPosition;
            _occupancy.Occupy(startCell);
            _rng = new System.Random(seed);
            _damageBonus = damageBonus;

            if (_health != null)
            {
                _health.OnDied += HandleDeath;
            }
        }

        private void TryMove(Vector2Int cell)
        {
            if (!_grid.IsWalkable(cell))
            {
                return;
            }

            if (!_occupancy.TryMove(_currentCell, cell))
            {
                return;
            }

            _targetCell = cell;
            _targetPosition = _gridPositionConverter.CellToWorld(cell);
            _isMoving = true;
            _currentPath.RemoveAt(0);
        }

        private void IdleStep()
        {
            var direction = RandomDirection();
            var candidate = _currentCell + direction;
            if (_grid.IsWalkable(candidate) && _occupancy.TryMove(_currentCell, candidate))
            {
                _targetCell = candidate;
                _targetPosition = _gridPositionConverter.CellToWorld(candidate);
                _isMoving = true;
            }
        }

        private void TryAttack(float distanceToPlayer)
        {
            if (distanceToPlayer > _enemyConfig.AttackRange || Time.time < _nextAttackTime || _targetHealth == null)
            {
                return;
            }

            _nextAttackTime = Time.time + _enemyConfig.AttackCooldown;
            var damage = _enemyConfig.BaseDamage + _damageBonus;
            _targetHealth.TakeDamage(damage);
        }

        private Vector2Int RandomDirection()
        {
            var roll = _rng.Next(0, 4);
            switch (roll)
            {
                case 0:
                    return Vector2Int.up;
                case 1:
                    return Vector2Int.right;
                case 2:
                    return Vector2Int.down;
                default:
                    return Vector2Int.left;
            }
        }

        private void HandleDeath()
        {
            _occupancy?.Release(_currentCell);
            Destroy(gameObject);
        }
    }
}
