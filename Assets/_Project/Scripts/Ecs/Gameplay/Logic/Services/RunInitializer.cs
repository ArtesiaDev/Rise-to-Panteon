using System;
using RuntimeRoguelike.Configs;
using UnityEngine;

namespace RuntimeRoguelike.Ecs
{
    public class RunInitializer
    {
        private readonly RunConfig _runConfig;
        private readonly MapGenerator _mapGenerator;
        private readonly HazardGenerator _hazardGenerator;
        private readonly PathfindingService _pathfindingService;
        private readonly EcsEntityFactory _entityFactory;
        private readonly EnemySpawnConfig _enemySpawnConfig;

        public RunInitializer(
            RunConfig runConfig,
            MapGenerator mapGenerator,
            HazardGenerator hazardGenerator,
            PathfindingService pathfindingService,
            EcsEntityFactory entityFactory,
            EnemySpawnConfig enemySpawnConfig)
        {
            _runConfig = runConfig;
            _mapGenerator = mapGenerator;
            _hazardGenerator = hazardGenerator;
            _pathfindingService = pathfindingService;
            _entityFactory = entityFactory;
            _enemySpawnConfig = enemySpawnConfig;
        }

        public void Initialize(EcsWorld world, bool clearWorld, bool randomizeSeed)
        {
            if (clearWorld)
            {
                world.Clear();
            }

            var seed = randomizeSeed ? GenerateSeed() : _runConfig.InitialSeed;
            var mapSize = _runConfig.MapSize;

            var generation = _mapGenerator.Generate(mapSize, seed);
            _hazardGenerator.Populate(generation.Grid, generation.StartCell, generation.SafeRadius, seed);

            var occupancy = new GridOccupancy(generation.Grid.Width, generation.Grid.Height);
            _pathfindingService.Configure(generation.Grid, occupancy);

            var runState = new RunState
            {
                Seed = seed,
                MapSize = new Int2(mapSize.x, mapSize.y),
                StartCell = new Int2(generation.StartCell.x, generation.StartCell.y),
                SafeRadius = generation.SafeRadius,
                IsInitialized = true
            };

            var counters = new RunCounters
            {
                Seed = seed,
                EnemyCount = 0,
                LootCount = 0,
                PlayerCell = runState.StartCell
            };

            var random = new RunRandom
            {
                Core = new Random(seed),
                EnemySpawn = new Random(seed + 1337),
                Loot = new Random(seed + 7777),
                Perks = new Random(seed + 2222)
            };

            world.SetResource(runState);
            world.SetResource(counters);
            world.SetResource(random);
            world.SetResource(new DifficultyState());
            world.SetResource(new PerkOfferState { IsVisible = false });
            world.SetResource(new RunCommands());
            world.SetResource(new DebugState());
            world.SetResource(new MapRenderRequest { Pending = true });
            world.SetResource(generation.Grid);
            world.SetResource(occupancy);

            var player = _entityFactory.CreatePlayer(world, runState.StartCell);
            occupancy.Occupy(runState.StartCell);
            counters.PlayerEntityId = player.Id;

            SpawnInitialEnemies(world, runState, occupancy, counters, random);
        }

        private int GenerateSeed()
        {
            return Guid.NewGuid().GetHashCode();
        }

        private void SpawnInitialEnemies(EcsWorld world, RunState runState, GridOccupancy occupancy, RunCounters counters, RunRandom rng)
        {
            if (rng == null || rng.EnemySpawn == null)
            {
                return;
            }

            if (!world.TryGetResource<MapGrid>(out var mapGrid))
            {
                return;
            }

            var attempts = Math.Max(1, _enemySpawnConfig.SpawnAttempts);
            for (var i = 0; i < _enemySpawnConfig.InitialCount; i++)
            {
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

                    _entityFactory.CreateEnemy(world, cell, 1f, 0);
                    occupancy.Occupy(cell);
                    counters.EnemyCount += 1;
                    break;
                }
            }
        }
    }
}
