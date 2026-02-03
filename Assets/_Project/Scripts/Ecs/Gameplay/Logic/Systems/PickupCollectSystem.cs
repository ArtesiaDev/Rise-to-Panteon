using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class PickupCollectSystem : IEcsInitSystem, IEcsFixedSystem
    {
        private EcsPool<GridPosition> _positionPool;
        private EcsPool<PlayerStatsComponent> _statsPool;
        private EcsPool<HealthComponent> _healthPool;
        private EcsPool<LootPickup> _lootPool;

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _positionPool = world.GetPool<GridPosition>();
            _statsPool = world.GetPool<PlayerStatsComponent>();
            _healthPool = world.GetPool<HealthComponent>();
            _lootPool = world.GetPool<LootPickup>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            var playerId = counters.PlayerEntityId;
            if (!_positionPool.Has(playerId))
            {
                return;
            }

            var playerCell = _positionPool.GetRef(playerId).Value;

            foreach (var lootEntity in world.Query<LootTag, GridPosition, LootPickup>())
            {
                var lootCell = _positionPool.GetRef(lootEntity).Value;
                if (lootCell != playerCell)
                {
                    continue;
                }

                ref var loot = ref _lootPool.GetRef(lootEntity);
                if (_statsPool.Has(playerId))
                {
                    ref var stats = ref _statsPool.GetRef(playerId);
                    if (loot.Type == PickupType.Gold)
                    {
                        stats.Gold += loot.Amount;
                    }
                    else if (loot.Type == PickupType.Xp)
                    {
                        stats.Xp += loot.Amount;
                    }
                }

                if (loot.Type == PickupType.Heal && _healthPool.Has(playerId))
                {
                    ref var health = ref _healthPool.GetRef(playerId);
                    health.Current = EcsMath.Max(0, health.Current + loot.Amount);
                    if (health.Current > health.Max)
                    {
                        health.Current = health.Max;
                    }
                }

                counters.LootCount = EcsMath.Max(0, counters.LootCount - 1);
                commandBuffer.AddComponent<DestroyedTag>(world.GetEntity(lootEntity), new DestroyedTag());
            }
        }
    }
}
