using System.Collections.Generic;
using UnityEngine;

namespace RuntimeRoguelike
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private int baseDamage = 1;
        [SerializeField] private float aggroRange = 8f;
        [SerializeField] private float attackRange = 1.1f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float pathRefreshInterval = 0.6f;
        [SerializeField] private float idleMoveInterval = 2.5f;

        private MapGrid grid;
        private GridOccupancy occupancy;
        private AStarPathfinder pathfinder;
        private PlayerController target;
        private Health targetHealth;
        private Health health;
        private System.Random rng;

        private Rigidbody2D body;
        private Vector2Int currentCell;
        private Vector2Int targetCell;
        private Vector2 targetPosition;
        private bool isMoving;
        private float nextPathTime;
        private float nextAttackTime;
        private float nextIdleMoveTime;
        private List<Vector2Int> currentPath = new List<Vector2Int>();

        private int damageBonus;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            if (grid == null || occupancy == null || target == null)
            {
                return;
            }

            var distanceToPlayer = Mathf.Abs(target.CurrentCell.x - currentCell.x) + Mathf.Abs(target.CurrentCell.y - currentCell.y);
            if (distanceToPlayer <= aggroRange)
            {
                if (Time.time >= nextPathTime)
                {
                    currentPath = pathfinder.FindPath(currentCell, target.CurrentCell);
                    nextPathTime = Time.time + pathRefreshInterval;
                }

                TryAttack(distanceToPlayer);

                if (!isMoving && currentPath.Count > 0)
                {
                    TryMove(currentPath[0]);
                }
            }
            else if (Time.time >= nextIdleMoveTime && !isMoving)
            {
                IdleStep();
                nextIdleMoveTime = Time.time + idleMoveInterval;
            }
        }

        private void FixedUpdate()
        {
            if (!isMoving)
            {
                return;
            }

            var nextPosition = Vector2.MoveTowards(body.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            body.MovePosition(nextPosition);

            if (Vector2.Distance(nextPosition, targetPosition) <= 0.01f)
            {
                body.MovePosition(targetPosition);
                currentCell = targetCell;
                isMoving = false;
            }
        }

        public void Initialize(MapGrid mapGrid, GridOccupancy gridOccupancy, AStarPathfinder aStar, PlayerController player, Vector2Int startCell, int seed, int damageBonusValue)
        {
            grid = mapGrid;
            occupancy = gridOccupancy;
            pathfinder = aStar;
            target = player;
            targetHealth = player.GetComponent<Health>();
            currentCell = startCell;
            targetCell = startCell;
            targetPosition = GridUtils.CellToWorld(startCell);
            body.position = targetPosition;
            occupancy.Occupy(startCell);
            rng = new System.Random(seed);
            damageBonus = damageBonusValue;

            if (health != null)
            {
                health.OnDied += HandleDeath;
            }
        }

        private void TryMove(Vector2Int cell)
        {
            if (!grid.IsWalkable(cell))
            {
                return;
            }

            if (!occupancy.TryMove(currentCell, cell))
            {
                return;
            }

            targetCell = cell;
            targetPosition = GridUtils.CellToWorld(cell);
            isMoving = true;
            currentPath.RemoveAt(0);
        }

        private void IdleStep()
        {
            var direction = RandomDirection();
            var candidate = currentCell + direction;
            if (grid.IsWalkable(candidate) && occupancy.TryMove(currentCell, candidate))
            {
                targetCell = candidate;
                targetPosition = GridUtils.CellToWorld(candidate);
                isMoving = true;
            }
        }

        private void TryAttack(float distanceToPlayer)
        {
            if (distanceToPlayer > attackRange || Time.time < nextAttackTime || targetHealth == null)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            var damage = baseDamage + damageBonus;
            targetHealth.TakeDamage(damage);
        }

        private Vector2Int RandomDirection()
        {
            var roll = rng.Next(0, 4);
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
            occupancy?.Release(currentCell);
            Destroy(gameObject);
        }
    }
}
