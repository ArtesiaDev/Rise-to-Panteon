using Unity.Burst;
using Unity.Entities;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RestartSystem : ISystem
    {
        private EntityQuery _runEntities;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _runEntities = state.GetEntityQuery(ComponentType.ReadOnly<RunTag>());
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<RunCommand>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var command = SystemAPI.GetSingletonRW<RunCommand>();
            var hasInput = SystemAPI.HasSingleton<InputState>();
            var input = hasInput ? SystemAPI.GetSingletonRW<InputState>() : default;

            if (!command.ValueRO.Restart && (!hasInput || !input.ValueRO.RestartPressed))
            {
                return;
            }

            state.Dependency.Complete();

            var runState = SystemAPI.GetSingletonRW<RunState>();
            runState.ValueRW.RunId += 1;
            runState.ValueRW.IsInitialized = false;

            if (SystemAPI.HasSingleton<MapBlobReference>())
            {
                var mapEntity = SystemAPI.GetSingletonEntity<RunState>();
                var mapRef = SystemAPI.GetComponent<MapBlobReference>(mapEntity);
                if (mapRef.Value.IsCreated)
                {
                    mapRef.Value.Dispose();
                }

                SystemAPI.SetComponent(mapEntity, new MapBlobReference());
            }

            if (SystemAPI.HasSingleton<RunSpawnState>())
            {
                SystemAPI.SetSingleton(new RunSpawnState { PlayerSpawned = false, InitialEnemiesSpawned = false });
            }

            if (SystemAPI.HasSingleton<EnemySpawnState>())
            {
                SystemAPI.SetSingleton(new EnemySpawnState { Timer = 0f });
            }

            if (SystemAPI.HasSingleton<RngState>())
            {
                SystemAPI.SetSingleton(new RngState { IsInitialized = false });
            }

            if (SystemAPI.HasSingleton<DifficultyState>())
            {
                SystemAPI.SetSingleton(new DifficultyState());
            }

            if (SystemAPI.HasSingleton<PerkOfferState>())
            {
                SystemAPI.SetSingleton(new PerkOfferState { IsVisible = false });
                var mapEntity = SystemAPI.GetSingletonEntity<RunState>();
                if (state.EntityManager.HasBuffer<PerkOption>(mapEntity))
                {
                    state.EntityManager.GetBuffer<PerkOption>(mapEntity).Clear();
                }
            }

            if (SystemAPI.HasSingleton<MapRenderRequest>())
            {
                SystemAPI.SetSingleton(new MapRenderRequest { RunId = runState.ValueRO.RunId });
                var mapEntity = SystemAPI.GetSingletonEntity<RunState>();
                state.EntityManager.SetComponentEnabled<MapRenderRequest>(mapEntity, true);
            }

            if (hasInput)
            {
                input.ValueRW = default;
            }

            command.ValueRW = default;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            ecb.DestroyEntity(_runEntities);
        }
    }
}
