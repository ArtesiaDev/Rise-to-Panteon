using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(LootPickupSystem))]
    public partial struct DeathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PrefabConfigData>();
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<MapBlobReference>();
            state.RequireForUpdate<LootConfigData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var mapRef = SystemAPI.GetSingleton<MapBlobReference>();
            if (!mapRef.Value.IsCreated)
            {
                return;
            }

            ref var map = ref mapRef.Value.Value;
            var occupancy = state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>());
            var lootConfig = SystemAPI.GetSingleton<LootConfigData>();
            var lootTable = SystemAPI.GetSingletonBuffer<LootEntryData>();
            var prefabConfig = SystemAPI.HasSingleton<PrefabConfigData>()
                ? SystemAPI.GetSingleton<PrefabConfigData>()
                : new PrefabConfigData { Loot = Entity.Null };

            var rngState = SystemAPI.GetSingletonRW<RngState>();
            var rng = rngState.ValueRW.Rng;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (health, position, entity) in SystemAPI.Query<RefRO<Health>, RefRO<GridPosition>>().WithEntityAccess())
            {
                if (health.ValueRO.Current > 0)
                {
                    continue;
                }

                var cell = position.ValueRO.Value;
                if (MapUtilities.InBounds(cell, map.Size))
                {
                    var index = MapUtilities.ToIndex(cell, map.Size);
                    if (occupancy[index].Value == entity)
                    {
                        occupancy[index] = new CellOccupant { Value = Entity.Null };
                    }
                }

                if (SystemAPI.HasComponent<EnemyTag>(entity))
                {
                    TryDropLoot(ecb, prefabConfig, cell, lootConfig, lootTable, ref rng);
                }

                ecb.DestroyEntity(entity);
            }

            rngState.ValueRW.Rng = rng;
        }

        private static void TryDropLoot(EntityCommandBuffer ecb, PrefabConfigData prefabConfig, int2 cell, LootConfigData config, DynamicBuffer<LootEntryData> table, ref Random rng)
        {
            if (table.Length == 0 || rng.NextFloat() > config.DropChance)
            {
                return;
            }

            var totalWeight = 0;
            for (var i = 0; i < table.Length; i++)
            {
                totalWeight += math.max(0, table[i].Weight);
            }

            var roll = rng.NextInt(0, math.max(1, totalWeight));
            var cumulative = 0;
            var entry = table[0];
            for (var i = 0; i < table.Length; i++)
            {
                cumulative += math.max(0, table[i].Weight);
                if (roll < cumulative)
                {
                    entry = table[i];
                    break;
                }
            }

            var lootEntity = prefabConfig.Loot != Entity.Null
                ? ecb.Instantiate(prefabConfig.Loot)
                : ecb.CreateEntity();

            ecb.AddComponent(lootEntity, new LootTag());
            ecb.AddComponent(lootEntity, new RunTag());
            ecb.AddComponent(lootEntity, new SpriteKeyComponent { Value = DotsSpriteKey.Loot });
            ecb.AddComponent(lootEntity, new GridPosition { Value = cell });
            ecb.AddComponent(lootEntity, new PreviousGridPosition { Value = cell });
            ecb.AddComponent(lootEntity, new RenderPosition { Value = new float2(cell.x, cell.y) });
            ecb.AddComponent(lootEntity, new LootPickup { Type = entry.Type, Amount = entry.Amount });
        }
    }
}
