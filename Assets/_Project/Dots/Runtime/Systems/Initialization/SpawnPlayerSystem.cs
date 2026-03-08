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

            var em = state.EntityManager;
            EntityUtilities.EnsureComponent(em, entity, new PlayerTag());
            EntityUtilities.EnsureComponent(em, entity, new RunTag());
            EntityUtilities.EnsureComponent(em, entity, new SpriteKeyComponent { Value = DotsSpriteKey.Player });
            EntityUtilities.EnsureComponent(em, entity, new GridPosition { Value = startCell });
            EntityUtilities.EnsureComponent(em, entity, new PreviousGridPosition { Value = startCell });
            EntityUtilities.EnsureComponent(em, entity, new RenderPosition { Value = new float2(startCell.x, startCell.y) });
            EntityUtilities.EnsureComponent(em, entity, new MoveSpeed { CellsPerSecond = playerConfig.MoveSpeed });
            EntityUtilities.EnsureComponent(em, entity, new MoveCooldown { Remaining = 0f });
            EntityUtilities.EnsureComponent(em, entity, new LastMoveDirection { Value = new int2(1, 0) });
            EntityUtilities.EnsureComponent(em, entity, new Health { Current = playerConfig.MaxHealth, Max = playerConfig.MaxHealth });
            EntityUtilities.EnsureComponent(em, entity, new Damage { Value = attackConfig.BaseDamage });
            EntityUtilities.EnsureComponent(em, entity, new AttackCooldown { Remaining = 0f, Interval = attackConfig.AttackCooldown });
            EntityUtilities.EnsureComponent(em, entity, new AttackRange { Value = attackConfig.AttackRange });
            EntityUtilities.EnsureComponent(em, entity, new HazardState { Current = HazardType.None, SpikeTickRemaining = 0f });
            EntityUtilities.EnsureComponent(em, entity, new PlayerStats
            {
                Level = 1,
                XpToNext = 0,
                MoveSpeedMult = 1f,
                BonusDamage = 0f,
                Gold = 0,
                Xp = 0
            });

            EntityUtilities.EnsureComponent(em, entity, new MoveIntent { Direction = int2.zero });
            em.SetComponentEnabled<MoveIntent>(entity, false);

            EntityUtilities.EnsureComponent(em, entity, new AttackRequest());
            em.SetComponentEnabled<AttackRequest>(entity, false);

            // Переполучаем handles после структурных изменений (CreateEntity/AddComponent)
            var occupancy = SystemAPI.GetSingletonBuffer<CellOccupant>();
            var index = MapUtilities.ToIndex(startCell, mapRef.Value.Value.Size);
            occupancy[index] = new CellOccupant { Value = entity };

            SystemAPI.GetSingletonRW<RunSpawnState>().ValueRW.PlayerSpawned = true;
        }
    }
}
