using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PerkApplySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RunCommand>();
            state.RequireForUpdate<PerkOfferState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var command = SystemAPI.GetSingletonRW<RunCommand>();
            if (!command.ValueRO.PerkChosen)
            {
                return;
            }

            var offerState = SystemAPI.GetSingletonRW<PerkOfferState>();
            if (!offerState.ValueRO.IsVisible)
            {
                command.ValueRW.PerkChosen = false;
                return;
            }

            var offerBuffer = SystemAPI.GetSingletonBuffer<PerkOption>();
            if (offerBuffer.Length == 0)
            {
                offerState.ValueRW.IsVisible = false;
                command.ValueRW.PerkChosen = false;
                return;
            }

            var chosenIndex = math.clamp(command.ValueRO.ChosenPerkIndex, 0, offerBuffer.Length - 1);
            var perkTable = SystemAPI.GetSingletonBuffer<PerkData>();
            var perkId = offerBuffer[chosenIndex].PerkId;
            if (perkId < 0 || perkId >= perkTable.Length)
            {
                offerState.ValueRW.IsVisible = false;
                offerBuffer.Clear();
                command.ValueRW.PerkChosen = false;
                return;
            }

            var perk = perkTable[perkId];

            foreach (var (stats, health) in SystemAPI.Query<RefRW<PlayerStats>, RefRW<Health>>().WithAll<PlayerTag>())
            {
                ApplyPerk(perk, ref stats.ValueRW, ref health.ValueRW);
                break;
            }

            offerState.ValueRW.IsVisible = false;
            offerBuffer.Clear();
            command.ValueRW.PerkChosen = false;
        }

        private static void ApplyPerk(PerkData perk, ref PlayerStats stats, ref Health health)
        {
            switch (perk.Type)
            {
                case PerkType.MaxHp:
                {
                    var hpBonus = math.max(1, (int)math.round(perk.Value));
                    health.Max += hpBonus;
                    health.Current = health.Max;
                    break;
                }
                case PerkType.Damage:
                    stats.BonusDamage += math.max(1, (int)math.round(perk.Value));
                    break;
                case PerkType.MoveSpeed:
                    stats.MoveSpeedMult = math.max(0.1f, stats.MoveSpeedMult + perk.Value);
                    break;
            }
        }
    }
}
