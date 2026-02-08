using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
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

            var attempts = math.max(1, spawnConfig.SpawnAttempts);
            var rng = Random.CreateFromIndex(runState.Seed + 1337u);

            var occupancy = state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>());
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

                    var enemy = SpawnEnemy(state.EntityManager, cell, enemyConfig, difficulty.EnemyMultiplier, 0);
                    occupancy[index] = new CellOccupant { Value = enemy };
                    break;
                }
            }

            spawnState.ValueRW.InitialEnemiesSpawned = true;
        }

        private static Entity SpawnEnemy(EntityManager entityManager, int2 cell, EnemyConfigData config, float difficultyMultiplier, int damageBonus)
        {
            var entity = entityManager.CreateEntity();

            EnsureComponent(entityManager, entity, new EnemyTag());
            EnsureComponent(entityManager, entity, new RunTag());
            EnsureComponent(entityManager, entity, new SpriteKeyComponent { Value = DotsSpriteKey.Enemy });
            EnsureComponent(entityManager, entity, new GridPosition { Value = cell });
            EnsureComponent(entityManager, entity, new RenderPosition { Value = new float2(cell.x, cell.y) });
            EnsureComponent(entityManager, entity, new MoveSpeed { CellsPerSecond = config.MoveSpeed });
            EnsureComponent(entityManager, entity, new MoveCooldown { Remaining = 0f });

            var maxHp = math.max(1, (int)math.round(config.MaxHealth * difficultyMultiplier));
            EnsureComponent(entityManager, entity, new Health { Max = maxHp, Current = maxHp });
            EnsureComponent(entityManager, entity, new Damage { Value = config.BaseDamage + damageBonus });
            EnsureComponent(entityManager, entity, new AggroRange { Value = config.AggroRange });
            EnsureComponent(entityManager, entity, new AttackRange { Value = config.AttackRange });
            EnsureComponent(entityManager, entity, new AttackCooldown { Remaining = 0f, Interval = config.AttackCooldown });
            EnsureComponent(entityManager, entity, new PathRefreshCooldown { Remaining = 0f, Interval = config.PathRefreshInterval });
            EnsureComponent(entityManager, entity, new IdleMoveCooldown { Remaining = config.IdleMoveInterval, Interval = config.IdleMoveInterval });
            EnsureComponent(entityManager, entity, new PathIndex { Value = 0 });
            EnsureComponent(entityManager, entity, new Target { Value = Entity.Null, HasTarget = false });
            EnsureComponent(entityManager, entity, new HazardState { Current = HazardType.None, SpikeTickRemaining = 0f });

            if (!entityManager.HasComponent<PathStep>(entity))
            {
                entityManager.AddBuffer<PathStep>(entity);
            }
            else
            {
                entityManager.GetBuffer<PathStep>(entity).Clear();
            }

            EnsureComponent(entityManager, entity, new MoveIntent { Direction = int2.zero });
            entityManager.SetComponentEnabled<MoveIntent>(entity, false);

            EnsureComponent(entityManager, entity, new AttackRequest());
            entityManager.SetComponentEnabled<AttackRequest>(entity, false);

            return entity;
        }

        private static void EnsureComponent<T>(EntityManager entityManager, Entity entity, T data)
            where T : unmanaged, IComponentData
        {
            if (entityManager.HasComponent<T>(entity))
            {
                entityManager.SetComponentData(entity, data);
            }
            else
            {
                entityManager.AddComponentData(entity, data);
            }
        }
    }
}
