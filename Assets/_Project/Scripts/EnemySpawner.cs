using UnityEngine;

namespace RuntimeRoguelike
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private int initialCount = 12;
        [SerializeField] private int maxEnemies = 40;
        [SerializeField] private float baseSpawnInterval = 6f;
        [SerializeField] private int spawnAttempts = 200;

        private MapGrid grid;
        private GridOccupancy occupancy;
        private AStarPathfinder pathfinder;
        private PlayerController player;
        private DifficultyDirector difficulty;
        private Transform parent;
        private System.Random rng;
        private Vector2Int safeCenter;
        private int safeRadius;
        private RunStats runStats;
        private int activeEnemies;
        private float nextSpawnTime;

        public int ActiveEnemyCount => activeEnemies;

        public void Configure(int initial, int max, float interval, int attempts)
        {
            initialCount = Mathf.Max(0, initial);
            maxEnemies = Mathf.Max(initialCount, max);
            baseSpawnInterval = Mathf.Max(0.5f, interval);
            spawnAttempts = Mathf.Max(10, attempts);
        }

        public void Initialize(MapGrid mapGrid, GridOccupancy gridOccupancy, AStarPathfinder aStar, PlayerController target, Transform entitiesRoot, Vector2Int safeCenterCell, int safeRadiusValue, DifficultyDirector difficultyDirector, RunStats stats, int seed)
        {
            grid = mapGrid;
            occupancy = gridOccupancy;
            pathfinder = aStar;
            player = target;
            parent = entitiesRoot;
            safeCenter = safeCenterCell;
            safeRadius = safeRadiusValue;
            difficulty = difficultyDirector;
            runStats = stats;
            rng = new System.Random(seed + 1337);
            activeEnemies = 0;

            SpawnInitial();
            nextSpawnTime = Time.time + baseSpawnInterval;
        }

        private void Update()
        {
            if (grid == null || occupancy == null)
            {
                return;
            }

            if (activeEnemies >= maxEnemies)
            {
                return;
            }

            if (Time.time < nextSpawnTime)
            {
                return;
            }

            if (TrySpawnEnemy())
            {
                var multiplier = difficulty != null ? difficulty.GetSpawnRateMultiplier(player != null ? player.GetComponent<PlayerStats>() : null) : 1f;
                nextSpawnTime = Time.time + baseSpawnInterval / Mathf.Max(0.1f, multiplier);
            }
            else
            {
                nextSpawnTime = Time.time + 1f;
            }
        }

        private void SpawnInitial()
        {
            for (var i = 0; i < initialCount; i++)
            {
                TrySpawnEnemy();
            }
        }

        private bool TrySpawnEnemy()
        {
            for (var attempt = 0; attempt < spawnAttempts; attempt++)
            {
                var cell = new Vector2Int(rng.Next(1, grid.Width - 1), rng.Next(1, grid.Height - 1));
                if (!grid.IsWalkable(cell))
                {
                    continue;
                }

                if (occupancy.IsOccupied(cell))
                {
                    continue;
                }

                var dx = cell.x - safeCenter.x;
                var dy = cell.y - safeCenter.y;
                if (dx * dx + dy * dy <= safeRadius * safeRadius)
                {
                    continue;
                }

                SpawnEnemy(cell);
                return true;
            }

            return false;
        }

        private void SpawnEnemy(Vector2Int cell)
        {
            var enemyObject = new GameObject("Enemy");
            enemyObject.transform.SetParent(parent, false);
            enemyObject.transform.position = GridUtils.CellToWorld(cell);

            var renderer = enemyObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Get(SpriteKey.Enemy);

            var body = enemyObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            var collider = enemyObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * 0.8f;

            var health = enemyObject.AddComponent<Health>();
            var stats = player != null ? player.GetComponent<PlayerStats>() : null;
            var difficultyMultiplier = difficulty != null ? difficulty.GetEnemyStatMultiplier(stats) : 1f;
            var maxHp = Mathf.RoundToInt(4 * difficultyMultiplier);
            health.Initialize(maxHp, Faction.Enemy);

            var ai = enemyObject.AddComponent<EnemyAI>();
            var damageBonus = Mathf.RoundToInt((difficultyMultiplier - 1f) * 2f);
            ai.Initialize(grid, occupancy, pathfinder, player, cell, rng.Next(), damageBonus);

            var dropper = enemyObject.AddComponent<LootDropper>();
            dropper.Initialize(rng, parent, runStats);

            health.OnDied += () =>
            {
                activeEnemies = Mathf.Max(0, activeEnemies - 1);
                runStats?.DecrementEnemy();
            };

            activeEnemies += 1;
            runStats?.IncrementEnemy();
        }
    }
}
