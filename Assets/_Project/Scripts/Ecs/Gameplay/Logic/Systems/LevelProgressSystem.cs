using RuntimeRoguelike.Configs;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class LevelProgressSystem : IEcsInitSystem, IEcsUpdateSystem
    {
        private readonly LevelConfig _levelConfig;
        private readonly PerkConfig _perkConfig;

        public LevelProgressSystem(LevelConfig levelConfig, PerkConfig perkConfig)
        {
            _levelConfig = levelConfig;
            _perkConfig = perkConfig;
        }

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            var statsPool = world.GetPool<PlayerStatsComponent>();
            foreach (var entity in world.Query<PlayerTag, PlayerStatsComponent>())
            {
                ref var stats = ref statsPool.GetRef(entity);
                if (stats.XpToNext <= 0)
                {
                    stats.XpToNext = GetXpForLevel(stats.Level);
                }
            }
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<PerkOfferState>(out var offer))
            {
                return;
            }

            var statsPool = world.GetPool<PlayerStatsComponent>();
            foreach (var entity in world.Query<PlayerTag, PlayerStatsComponent>())
            {
                ref var stats = ref statsPool.GetRef(entity);
                var leveled = false;
                while (stats.Xp >= stats.XpToNext)
                {
                    stats.Xp -= stats.XpToNext;
                    stats.Level += 1;
                    stats.XpToNext = GetXpForLevel(stats.Level);
                    leveled = true;
                }

                if (leveled && !offer.IsVisible)
                {
                    offer.Options = PickRandomPerks(world);
                    offer.IsVisible = true;
                }
            }
        }

        private PerkDefinition[] PickRandomPerks(EcsWorld world)
        {
            if (_perkConfig.Perks == null || _perkConfig.Perks.Length == 0)
            {
                return new PerkDefinition[0];
            }

            var count = EcsMath.Max(1, _perkConfig.ChoicesCount);
            if (count > _perkConfig.Perks.Length)
            {
                count = _perkConfig.Perks.Length;
            }

            var indices = new int[_perkConfig.Perks.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            var rng = world.TryGetResource<RunRandom>(out var random) ? random.Perks : null;
            for (var i = 0; i < indices.Length; i++)
            {
                var swap = rng != null ? rng.Next(i, indices.Length) : i;
                var temp = indices[i];
                indices[i] = indices[swap];
                indices[swap] = temp;
            }

            var result = new PerkDefinition[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = _perkConfig.Perks[indices[i]];
            }

            return result;
        }

        private int GetXpForLevel(int level)
        {
            return _levelConfig.BaseXpToLevel + EcsMath.Max(0, level - 1) * _levelConfig.XpIncreasePerLevel;
        }
    }
}
