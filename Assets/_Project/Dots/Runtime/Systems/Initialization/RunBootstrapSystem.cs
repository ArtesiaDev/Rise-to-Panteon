using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    public partial struct RunBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<RunState>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new RunState
                {
                    Seed = 0u,
                    MapSize = int2.zero,
                    StartCell = int2.zero,
                    SafeRadius = 0,
                    RunId = 0,
                    IsInitialized = false,
                    FixedStepApplied = false
                });

                state.EntityManager.AddComponentData(entity, new DifficultyState
                {
                    ElapsedTime = 0f,
                    EnemyMultiplier = 1f,
                    SpawnRateMultiplier = 1f
                });

                state.EntityManager.AddComponentData(entity, new InputState());
                state.EntityManager.AddComponentData(entity, new RunCommand());
                state.EntityManager.AddComponentData(entity, new PerkOfferState { IsVisible = false });
                state.EntityManager.AddComponentData(entity, new RngState { IsInitialized = false });
                state.EntityManager.AddComponentData(entity, new RunSpawnState { PlayerSpawned = false, InitialEnemiesSpawned = false });
                state.EntityManager.AddComponentData(entity, new EnemySpawnState { Timer = 0f });
                state.EntityManager.AddComponentData(entity, new FixedStepSettings { Timestep = 0f, IsSet = false });
                state.EntityManager.AddBuffer<PerkOption>(entity);
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<RunState>() || !SystemAPI.HasSingleton<FixedStepSettings>())
            {
                return;
            }

            var runState = SystemAPI.GetSingletonRW<RunState>();
            if (runState.ValueRO.FixedStepApplied)
            {
                return;
            }

            var fixedSettings = SystemAPI.GetSingleton<FixedStepSettings>();
            if (!fixedSettings.IsSet || fixedSettings.Timestep <= 0f)
            {
                return;
            }

            var fixedGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            if (fixedGroup == null)
            {
                return;
            }

            fixedGroup.Timestep = fixedSettings.Timestep;
            runState.ValueRW.FixedStepApplied = true;
        }
    }
}
