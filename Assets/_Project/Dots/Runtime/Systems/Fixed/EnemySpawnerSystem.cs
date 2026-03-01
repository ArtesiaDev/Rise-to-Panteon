using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(DifficultyTickSystem))]
    public partial struct EnemySpawnerSystem : ISystem
    {
        private EntityQuery _enemyQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabConfigData>();
            state.RequireForUpdate<DifficultyState>();
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<RunSpawnState>();
            state.RequireForUpdate<EnemySpawnState>();
            state.RequireForUpdate<EnemySpawnConfigData>();
            state.RequireForUpdate<EnemyConfigData>();
            state.RequireForUpdate<MapBlobReference>();

            _enemyQuery = state.GetEntityQuery(ComponentType.ReadOnly<EnemyTag>());
        }

        public void OnUpdate(ref SystemState state)
        {
            var runState = SystemAPI.GetSingleton<RunState>();
            var spawnState = SystemAPI.GetSingleton<RunSpawnState>();
            if (!runState.IsInitialized || !spawnState.InitialEnemiesSpawned) return;

            var mapRef = SystemAPI.GetSingleton<MapBlobReference>();
            if (!mapRef.Value.IsCreated) return;

            ref var map = ref mapRef.Value.Value;
            var spawnConfig = SystemAPI.GetSingleton<EnemySpawnConfigData>();
            var enemyConfig = SystemAPI.GetSingleton<EnemyConfigData>();
            var difficulty = SystemAPI.HasSingleton<DifficultyState>()
                ? SystemAPI.GetSingleton<DifficultyState>()
                : new DifficultyState { EnemyMultiplier = 1f, SpawnRateMultiplier = 1f };

            var timer = SystemAPI.GetSingletonRW<EnemySpawnState>();
            timer.ValueRW.Timer -= SystemAPI.Time.DeltaTime;
            if (timer.ValueRO.Timer > 0f) return;

            var enemyCount = _enemyQuery.CalculateEntityCount();
            if (enemyCount >= spawnConfig.MaxCount)
            {
                timer.ValueRW.Timer = spawnConfig.SpawnInterval;
                return;
            }

            var occupancy = SystemAPI.GetSingletonBuffer<CellOccupant>();
            var rng = SystemAPI.GetSingletonRW<RngState>().ValueRW.Rng;

            var attempts = math.max(1, spawnConfig.SpawnAttempts);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var cell = new int2(
                    rng.NextInt(1, map.Size.x - 1),
                    rng.NextInt(1, map.Size.y - 1));

                if (!MapUtilities.IsWalkable(ref map, cell))
                {
                    continue;
                }

                var index = MapUtilities.ToIndex(cell, map.Size);
                if (occupancy[index].Value != Entity.Null)
                {
                    continue;
                }

                var dx = cell.x - runState.StartCell.x;
                var dy = cell.y - runState.StartCell.y;
                if (dx * dx + dy * dy <= runState.SafeRadius * runState.SafeRadius)
                {
                    continue;
                }

                var prefabConfig = SystemAPI.HasSingleton<PrefabConfigData>()
                    ? SystemAPI.GetSingleton<PrefabConfigData>()
                    : new PrefabConfigData { Enemy = Entity.Null };

                var enemy = prefabConfig.Enemy != Entity.Null
                    ? state.EntityManager.Instantiate(prefabConfig.Enemy)
                    : state.EntityManager.CreateEntity();

                EnemySpawnUtilities.InitializeEnemy(state.EntityManager, enemy, cell, enemyConfig, difficulty.EnemyMultiplier, 0);

                // Переполучаем handles после структурных изменений (CreateEntity/AddComponent)
                occupancy = SystemAPI.GetSingletonBuffer<CellOccupant>();
                occupancy[index] = new CellOccupant { Value = enemy };
                break;
            }

            // Переполучаем handles после структурных изменений
            SystemAPI.GetSingletonRW<RngState>().ValueRW.Rng = rng;

            var interval = spawnConfig.SpawnInterval;
            if (difficulty.SpawnRateMultiplier > 0f)
            {
                interval = spawnConfig.SpawnInterval / difficulty.SpawnRateMultiplier;
            }

            SystemAPI.GetSingletonRW<EnemySpawnState>().ValueRW.Timer = math.max(0.05f, interval);
        }
    }
}
