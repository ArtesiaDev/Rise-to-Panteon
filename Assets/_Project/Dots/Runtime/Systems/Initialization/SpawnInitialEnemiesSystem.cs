using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SpawnPlayerSystem))]
    public partial struct SpawnInitialEnemiesSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DifficultyState>();
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<RunSpawnState>();
            state.RequireForUpdate<EnemyConfigData>();
            state.RequireForUpdate<EnemySpawnConfigData>();
            state.RequireForUpdate<MapBlobReference>();
            state.RequireForUpdate<PrefabConfigData>();
            state.RequireForUpdate<RngState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var runState = SystemAPI.GetSingleton<RunState>();
            var spawnState = SystemAPI.GetSingletonRW<RunSpawnState>();
            if (!runState.IsInitialized || spawnState.ValueRO.InitialEnemiesSpawned)
            {
                return;
            }

            var mapRef = SystemAPI.GetSingleton<MapBlobReference>();
            if (!mapRef.Value.IsCreated)
            {
                return;
            }

            ref var map = ref mapRef.Value.Value;
            var enemyConfig = SystemAPI.GetSingleton<EnemyConfigData>();
            var spawnConfig = SystemAPI.GetSingleton<EnemySpawnConfigData>();
            var difficulty = SystemAPI.HasSingleton<DifficultyState>()
                ? SystemAPI.GetSingleton<DifficultyState>()
                : new DifficultyState { EnemyMultiplier = 1f };

            var prefabConfig = SystemAPI.GetSingleton<PrefabConfigData>();

            // Используем общий RngState вместо отдельного RNG
            var rngState = SystemAPI.GetSingletonRW<RngState>();
            var rng = rngState.ValueRW.Rng;

            var attempts = math.max(1, spawnConfig.SpawnAttempts);
            var occupancy = SystemAPI.GetSingletonBuffer<CellOccupant>();

            for (var i = 0; i < spawnConfig.InitialCount; i++)
            {
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

                    var enemy = prefabConfig.Enemy != Entity.Null
                        ? state.EntityManager.Instantiate(prefabConfig.Enemy)
                        : state.EntityManager.CreateEntity();

                    EnemySpawnUtilities.InitializeEnemy(state.EntityManager, enemy, cell, enemyConfig, difficulty.EnemyMultiplier, 0);
                    occupancy[index] = new CellOccupant { Value = enemy };
                    break;
                }
            }

            // Сохраняем состояние RNG обратно
            rngState.ValueRW.Rng = rng;
            spawnState.ValueRW.InitialEnemiesSpawned = true;
        }
    }
}
