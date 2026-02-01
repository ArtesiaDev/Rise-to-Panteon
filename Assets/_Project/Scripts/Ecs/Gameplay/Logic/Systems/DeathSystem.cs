using RuntimeRoguelike.Configs;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class DeathSystem : IEcsUpdateSystem
    {
        private readonly LootConfig _lootConfig;
        private readonly EcsEntityFactory _entityFactory;

        public DeathSystem(LootConfig lootConfig, EcsEntityFactory entityFactory)
        {
            _lootConfig = lootConfig;
            _entityFactory = entityFactory;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            var healthPool = world.GetPool<HealthComponent>();
            var destroyedPool = world.GetPool<DestroyedTag>();
            var positionPool = world.GetPool<GridPosition>();
            var enemyTagPool = world.GetPool<EnemyTag>();

            world.TryGetResource<GridOccupancy>(out var occupancy);
            world.TryGetResource<RunCounters>(out var counters);
            world.TryGetResource<RunRandom>(out var rng);

            foreach (var entity in world.Query<HealthComponent>())
            {
                ref var health = ref healthPool.GetRef(entity);
                if (health.Current > 0)
                {
                    continue;
                }

                if (destroyedPool.Has(entity))
                {
                    continue;
                }

                if (positionPool.Has(entity) && occupancy != null)
                {
                    occupancy.Release(positionPool.GetRef(entity).Value);
                }

                if (enemyTagPool.Has(entity) && counters != null)
                {
                    counters.EnemyCount = EcsMath.Max(0, counters.EnemyCount - 1);
                    TryDropLoot(world, positionPool, entity, rng, counters);
                }

                commandBuffer.AddComponent<DestroyedTag>(world.GetEntity(entity), new DestroyedTag());
            }
        }

        private void TryDropLoot(EcsWorld world, EcsPool<GridPosition> positionPool, int enemyId, RunRandom rng, RunCounters counters)
        {
            if (rng == null || rng.Loot == null || _lootConfig == null || _lootConfig.LootTable == null || _lootConfig.LootTable.Length == 0)
            {
                return;
            }

            if (rng.Loot.NextDouble() > _lootConfig.DropChance)
            {
                return;
            }

            if (!positionPool.Has(enemyId))
            {
                return;
            }

            var entry = PickEntry(rng);
            var cell = positionPool.GetRef(enemyId).Value;
            _entityFactory.CreateLoot(world, cell, entry.Type, entry.Amount);
            counters.LootCount += 1;
        }

        private LootEntry PickEntry(RunRandom rng)
        {
            var totalWeight = 0;
            for (var i = 0; i < _lootConfig.LootTable.Length; i++)
            {
                totalWeight += _lootConfig.LootTable[i].Weight;
            }

            var roll = rng.Loot.Next(0, EcsMath.Max(1, totalWeight));
            var cumulative = 0;
            for (var i = 0; i < _lootConfig.LootTable.Length; i++)
            {
                cumulative += _lootConfig.LootTable[i].Weight;
                if (roll < cumulative)
                {
                    return _lootConfig.LootTable[i];
                }
            }

            return _lootConfig.LootTable[0];
        }
    }
}
