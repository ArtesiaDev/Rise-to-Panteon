using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(MapGenerationSystem))]
    public partial struct SpawnPlayerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabConfigData>();
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<RunSpawnState>();
            state.RequireForUpdate<PlayerConfigData>();
            state.RequireForUpdate<PlayerAttackConfigData>();
            state.RequireForUpdate<MapBlobReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var runState = SystemAPI.GetSingleton<RunState>();
            var spawnState = SystemAPI.GetSingletonRW<RunSpawnState>();
            if (!runState.IsInitialized || spawnState.ValueRO.PlayerSpawned)
            {
                return;
            }

            var mapRef = SystemAPI.GetSingleton<MapBlobReference>();
            if (!mapRef.Value.IsCreated)
            {
                return;
            }

            var startCell = runState.StartCell;
            var playerConfig = SystemAPI.GetSingleton<PlayerConfigData>();
            var attackConfig = SystemAPI.GetSingleton<PlayerAttackConfigData>();

            var prefabConfig = SystemAPI.HasSingleton<PrefabConfigData>()
                ? SystemAPI.GetSingleton<PrefabConfigData>()
                : new PrefabConfigData { Player = Entity.Null };

            var entity = prefabConfig.Player != Entity.Null
                ? state.EntityManager.Instantiate(prefabConfig.Player)
                : state.EntityManager.CreateEntity();

            EnsureComponent(state.EntityManager, entity, new PlayerTag());
            EnsureComponent(state.EntityManager, entity, new RunTag());
            EnsureComponent(state.EntityManager, entity, new SpriteKeyComponent { Value = DotsSpriteKey.Player });
            EnsureComponent(state.EntityManager, entity, new GridPosition { Value = startCell });
            EnsureComponent(state.EntityManager, entity, new PreviousGridPosition { Value = startCell });
            EnsureComponent(state.EntityManager, entity, new RenderPosition { Value = new float2(startCell.x, startCell.y) });
            EnsureComponent(state.EntityManager, entity, new MoveSpeed { CellsPerSecond = playerConfig.MoveSpeed });
            EnsureComponent(state.EntityManager, entity, new MoveCooldown { Remaining = 0f });
            EnsureComponent(state.EntityManager, entity, new LastMoveDirection { Value = new int2(1, 0) });
            EnsureComponent(state.EntityManager, entity, new Health { Current = playerConfig.MaxHealth, Max = playerConfig.MaxHealth });
            EnsureComponent(state.EntityManager, entity, new Damage { Value = attackConfig.BaseDamage });
            EnsureComponent(state.EntityManager, entity, new AttackCooldown { Remaining = 0f, Interval = attackConfig.AttackCooldown });
            EnsureComponent(state.EntityManager, entity, new AttackRange { Value = attackConfig.AttackRange });
            EnsureComponent(state.EntityManager, entity, new HazardState { Current = HazardType.None, SpikeTickRemaining = 0f });
            EnsureComponent(state.EntityManager, entity, new PlayerStats
            {
                Level = 1,
                XpToNext = 0,
                MoveSpeedMult = 1f,
                BonusDamage = 0f,
                Gold = 0,
                Xp = 0
            });

            EnsureComponent(state.EntityManager, entity, new MoveIntent { Direction = int2.zero });
            state.EntityManager.SetComponentEnabled<MoveIntent>(entity, false);

            EnsureComponent(state.EntityManager, entity, new AttackRequest());
            state.EntityManager.SetComponentEnabled<AttackRequest>(entity, false);

            var occupancy = state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>());
            var index = MapUtilities.ToIndex(startCell, mapRef.Value.Value.Size);
            occupancy[index] = new CellOccupant { Value = entity };

            spawnState.ValueRW.PlayerSpawned = true;
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
