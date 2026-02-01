using System;
using RuntimeRoguelike.Configs;

namespace RuntimeRoguelike.Ecs
{
    public class EnemySpawnerSystem : IEcsUpdateSystem
    {
        private readonly EnemySpawnConfig _spawnConfig;
        private readonly EcsEntityFactory _entityFactory;
        private float _spawnTimer;
        private int _lastSeed = int.MinValue;

        public EnemySpawnerSystem(EnemySpawnConfig spawnConfig, EcsEntityFactory entityFactory)
        {
            _spawnConfig = spawnConfig;
            _entityFactory = entityFactory;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<RunState>(out var runState))
            {
                return;
            }

            if (runState.Seed != _lastSeed)
            {
                _lastSeed = runState.Seed;
                _spawnTimer = _spawnConfig.SpawnInterval;
            }

            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            if (counters.EnemyCount >= _spawnConfig.MaxCount)
            {
                return;
            }

            if (_spawnTimer <= 0f)
            {
                _spawnTimer = _spawnConfig.SpawnInterval;
            }

            _spawnTimer -= deltaTime;
            if (_spawnTimer > 0f)
            {
                return;
            }

            if (TrySpawnEnemy(world, runState))
            {
                var multiplier = 1f;
                if (world.TryGetResource<DifficultyState>(out var difficulty))
                {
                    multiplier = difficulty.SpawnRateMultiplier;
                }

                _spawnTimer = _spawnConfig.SpawnInterval / Math.Max(0.1f, multiplier);
            }
            else
            {
                _spawnTimer = 1f;
            }
        }

        private bool TrySpawnEnemy(EcsWorld world, RunState runState)
        {
            if (!world.TryGetResource<MapGrid>(out var mapGrid))
            {
                return false;
            }

            if (!world.TryGetResource<GridOccupancy>(out var occupancy))
            {
                return false;
            }

            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return false;
            }

            if (!world.TryGetResource<RunRandom>(out var rng) || rng.EnemySpawn == null)
            {
                return false;
            }

            var attempts = Math.Max(1, _spawnConfig.SpawnAttempts);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var x = rng.EnemySpawn.Next(1, mapGrid.Width - 1);
                var y = rng.EnemySpawn.Next(1, mapGrid.Height - 1);
                var cell = new Int2(x, y);

                if (!mapGrid.IsWalkable(cell))
                {
                    continue;
                }

                if (occupancy.IsOccupied(cell))
                {
                    continue;
                }

                var dx = cell.X - runState.StartCell.X;
                var dy = cell.Y - runState.StartCell.Y;
                if (dx * dx + dy * dy <= runState.SafeRadius * runState.SafeRadius)
                {
                    continue;
                }

                SpawnEnemy(world, cell, counters, occupancy);
                return true;
            }

            return false;
        }

        private void SpawnEnemy(EcsWorld world, Int2 cell, RunCounters counters, GridOccupancy occupancy)
        {
            var difficultyMultiplier = 1f;
            if (world.TryGetResource<DifficultyState>(out var difficulty))
            {
                difficultyMultiplier = difficulty.EnemyMultiplier;
            }

            var damageBonus = Math.Max(0, (int)Math.Round((difficultyMultiplier - 1f) * 2f));
            _entityFactory.CreateEnemy(world, cell, difficultyMultiplier, damageBonus);
            occupancy.Occupy(cell);
            counters.EnemyCount += 1;
        }
    }
}
