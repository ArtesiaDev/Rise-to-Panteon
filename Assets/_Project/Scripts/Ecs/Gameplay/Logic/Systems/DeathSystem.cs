using RuntimeRoguelike.Configs;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class DeathSystem : IEcsInitSystem, IEcsFixedSystem
    {
        private readonly LootConfig _lootConfig;
        private readonly EcsEntityFactory _entityFactory;
        private EcsPool<HealthComponent> _healthPool;
        private EcsPool<DestroyedTag> _destroyedPool;
        private EcsPool<GridPosition> _positionPool;
        private EcsPool<EnemyTag> _enemyTagPool;

        public DeathSystem(LootConfig lootConfig, EcsEntityFactory entityFactory)
        {
            _lootConfig = lootConfig;
            _entityFactory = entityFactory;
        }

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _healthPool = world.GetPool<HealthComponent>();
            _destroyedPool = world.GetPool<DestroyedTag>();
            _positionPool = world.GetPool<GridPosition>();
            _enemyTagPool = world.GetPool<EnemyTag>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            world.TryGetResource<GridOccupancy>(out var occupancy);
            world.TryGetResource<RunCounters>(out var counters);
            world.TryGetResource<RunRandom>(out var rng);

            foreach (var entity in world.Query<HealthComponent>())
            {
                ref var health = ref _healthPool.GetRef(entity);
                if (health.Current > 0)
                {
                    continue;
                }

                if (_destroyedPool.Has(entity))
                {
                    continue;
                }

                if (_positionPool.Has(entity) && occupancy != null)
                {
                    occupancy.Release(_positionPool.GetRef(entity).Value);
                }

                if (_enemyTagPool.Has(entity) && counters != null)
                {
                    counters.EnemyCount = EcsMath.Max(0, counters.EnemyCount - 1);
                    TryDropLoot(world, _positionPool, entity, rng, counters);
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
