using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class PerkApplySystem : IEcsInitSystem, IEcsUpdateSystem
    {
        private EcsPool<PlayerStatsComponent> _statsPool;
        private EcsPool<HealthComponent> _healthPool;

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _statsPool = world.GetPool<PlayerStatsComponent>();
            _healthPool = world.GetPool<HealthComponent>();
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<RunCommands>(out var commands))
            {
                return;
            }

            if (!commands.PerkChosen)
            {
                return;
            }

            if (!world.TryGetResource<PerkOfferState>(out var offer))
            {
                return;
            }

            foreach (var entity in world.Query<PlayerTag, PlayerStatsComponent>())
            {
                ref var stats = ref _statsPool.GetRef(entity);
                ref var health = ref _healthPool.GetRef(entity);
                ApplyPerk(commands.ChosenPerk, ref stats, ref health);
                break;
            }

            offer.IsVisible = false;
            offer.Options = null;
            commands.PerkChosen = false;
        }

        private void ApplyPerk(PerkDefinition perk, ref PlayerStatsComponent stats, ref HealthComponent health)
        {
            switch (perk.Type)
            {
                case PerkType.MaxHp:
                    var hpBonus = EcsMath.Max(1, EcsMath.RoundToInt(perk.Value));
                    health.Max += hpBonus;
                    health.Current = health.Max;
                    break;
                case PerkType.Damage:
                    stats.BonusDamage += EcsMath.Max(1, EcsMath.RoundToInt(perk.Value));
                    break;
                case PerkType.MoveSpeed:
                    stats.MoveSpeedMultiplier = EcsMath.Max(0.1f, stats.MoveSpeedMultiplier + perk.Value);
                    break;
            }
        }
    }
}
