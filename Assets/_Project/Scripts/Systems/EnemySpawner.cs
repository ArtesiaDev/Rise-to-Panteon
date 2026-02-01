using RuntimeRoguelike.Configs;
using UnityEngine;
using Zenject;

namespace RuntimeRoguelike
{
    public class EnemySpawner
    {
        private readonly EnemyFactory _enemyFactory;

        private MapGrid _grid;
        private GridOccupancy _occupancy;
        private AStarPathfinder _pathfinder;
        private PlayerController _player;
        private Transform _parent;
        private Vector2Int _safeCenter;
        private int _safeRadius;
        private EnemySpawnConfig _spawnConfig;
        private EnemyConfig _enemyConfig;
        private DifficultyService _difficulty;
        private LootConfig _lootConfig;
        private RunStats _runStats;
        private System.Random _rng;
        private int _activeEnemies;
        private float _nextSpawnTime;

        public int ActiveEnemyCount => _activeEnemies;

        [Inject]
        public EnemySpawner(EnemyFactory enemyFactory)
        {
            _enemyFactory = enemyFactory;
        }

        public void Initialize(
            MapGrid mapGrid,
            GridOccupancy occupancy,
            AStarPathfinder pathfinder,
            PlayerController player,
            Transform entitiesRoot,
            Vector2Int safeCenterCell,
            int safeRadiusValue,
            EnemySpawnConfig spawnConfig,
            EnemyConfig enemyConfig,
            DifficultyService difficulty,
            LootConfig lootConfig,
            RunStats runStats,
            int seed)
        {
            _grid = mapGrid;
            _occupancy = occupancy;
            _pathfinder = pathfinder;
            _player = player;
            _parent = entitiesRoot;
            _safeCenter = safeCenterCell;
            _safeRadius = safeRadiusValue;
            _spawnConfig = spawnConfig;
            _enemyConfig = enemyConfig;
            _difficulty = difficulty;
            _lootConfig = lootConfig;
            _runStats = runStats;
            _rng = new System.Random(seed + 1337);
            _activeEnemies = 0;

            SpawnInitial();
            _nextSpawnTime = Time.time + _spawnConfig.SpawnInterval;
        }

        public void Tick()
        {
            if (_grid == null || _occupancy == null)
            {
                return;
            }

            if (_activeEnemies >= _spawnConfig.MaxCount)
            {
                return;
            }

            if (Time.time < _nextSpawnTime)
            {
                return;
            }

            if (TrySpawnEnemy())
            {
                var multiplier = _difficulty != null ? _difficulty.GetSpawnRateMultiplier(_player != null ? _player.GetComponent<PlayerStats>() : null) : 1f;
                _nextSpawnTime = Time.time + _spawnConfig.SpawnInterval / Mathf.Max(0.1f, multiplier);
            }
            else
            {
                _nextSpawnTime = Time.time + 1f;
            }
        }

        private void SpawnInitial()
        {
            for (var i = 0; i < _spawnConfig.InitialCount; i++)
            {
                TrySpawnEnemy();
            }
        }

        private bool TrySpawnEnemy()
        {
            for (var attempt = 0; attempt < _spawnConfig.SpawnAttempts; attempt++)
            {
                var cell = new Vector2Int(_rng.Next(1, _grid.Width - 1), _rng.Next(1, _grid.Height - 1));
                if (!_grid.IsWalkable(cell))
                {
                    continue;
                }

                if (_occupancy.IsOccupied(cell))
                {
                    continue;
                }

                var dx = cell.x - _safeCenter.x;
                var dy = cell.y - _safeCenter.y;
                if (dx * dx + dy * dy <= _safeRadius * _safeRadius)
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
            var health = _enemyFactory.CreateEnemy(cell, _parent, _grid, _occupancy, _pathfinder, _player, _enemyConfig, _difficulty, _lootConfig, _runStats, _rng);
            health.OnDied += () =>
            {
                _activeEnemies = Mathf.Max(0, _activeEnemies - 1);
                _runStats?.DecrementEnemy();
            };

            _activeEnemies += 1;
            _runStats?.IncrementEnemy();
        }
    }
}
