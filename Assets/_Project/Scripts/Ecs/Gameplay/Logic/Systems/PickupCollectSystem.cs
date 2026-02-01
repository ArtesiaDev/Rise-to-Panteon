using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class PickupCollectSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            var playerId = counters.PlayerEntityId;
            var positionPool = world.GetPool<GridPosition>();
            if (!positionPool.Has(playerId))
            {
                return;
            }

            var playerCell = positionPool.GetRef(playerId).Value;
            var statsPool = world.GetPool<PlayerStatsComponent>();
            var healthPool = world.GetPool<HealthComponent>();
            var lootPool = world.GetPool<LootPickup>();

            foreach (var lootEntity in world.Query<LootTag, GridPosition, LootPickup>())
            {
                var lootCell = positionPool.GetRef(lootEntity).Value;
                if (lootCell != playerCell)
                {
                    continue;
                }

                ref var loot = ref lootPool.GetRef(lootEntity);
                if (statsPool.Has(playerId))
                {
                    ref var stats = ref statsPool.GetRef(playerId);
                    if (loot.Type == PickupType.Gold)
                    {
                        stats.Gold += loot.Amount;
                    }
                    else if (loot.Type == PickupType.Xp)
                    {
                        stats.Xp += loot.Amount;
                    }
                }

                if (loot.Type == PickupType.Heal && healthPool.Has(playerId))
                {
                    ref var health = ref healthPool.GetRef(playerId);
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
