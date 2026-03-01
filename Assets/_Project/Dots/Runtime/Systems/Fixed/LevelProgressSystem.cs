using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(DeathSystem))]
    public partial struct LevelProgressSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelConfigData>();
            state.RequireForUpdate<PerkConfigData>();
            state.RequireForUpdate<PerkOfferState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var levelConfig = SystemAPI.GetSingleton<LevelConfigData>();
            var perkConfig = SystemAPI.GetSingleton<PerkConfigData>();
            var perkTable = SystemAPI.GetSingletonBuffer<PerkData>();
            var offerState = SystemAPI.GetSingletonRW<PerkOfferState>();
            var offerBuffer = SystemAPI.GetSingletonBuffer<PerkOption>();

            var rngState = SystemAPI.GetSingletonRW<RngState>();
            var rng = rngState.ValueRW.Rng;

            foreach (var stats in SystemAPI.Query<RefRW<PlayerStats>>().WithAll<PlayerTag>())
            {
                if (stats.ValueRO.XpToNext <= 0)
                {
                    stats.ValueRW.XpToNext = GetXpForLevel(stats.ValueRO.Level, levelConfig);
                }

                var leveled = false;
                while (stats.ValueRO.Xp >= stats.ValueRO.XpToNext)
                {
                    stats.ValueRW.Xp -= stats.ValueRO.XpToNext;
                    stats.ValueRW.Level += 1;
                    stats.ValueRW.XpToNext = GetXpForLevel(stats.ValueRO.Level, levelConfig);
                    leveled = true;
                }

                if (leveled && !offerState.ValueRO.IsVisible)
                {
                    FillPerkOffer(perkConfig, perkTable, ref rng, offerBuffer);
                    offerState.ValueRW.IsVisible = true;
                }
            }

            rngState.ValueRW.Rng = rng;
        }

        private static void FillPerkOffer(PerkConfigData config, DynamicBuffer<PerkData> perks, ref Random rng, DynamicBuffer<PerkOption> offerBuffer)
        {
            offerBuffer.Clear();
            if (perks.Length == 0)
            {
                return;
            }

            var count = math.max(1, config.ChoicesCount);
            if (count > perks.Length)
            {
                count = perks.Length;
            }
            
            var indices = new NativeArray<int>(perks.Length, Allocator.Temp);

            try
            {
                for (var i = 0; i < indices.Length; i++)
                {
                    indices[i] = i;
                }

                for (var i = 0; i < indices.Length; i++)
                {
                    var swap = rng.NextInt(i, indices.Length);
                    (indices[i], indices[swap]) = (indices[swap], indices[i]);
                }

                for (var i = 0; i < count; i++)
                {
                    offerBuffer.Add(new PerkOption { PerkId = indices[i] });
                }
            }
            finally
            {
                indices.Dispose();
            }
        }

        private static int GetXpForLevel(int level, LevelConfigData config)
        {
            return config.BaseXpToLevel + math.max(0, level - 1) * config.XpIncreasePerLevel;
        }
    }
}
