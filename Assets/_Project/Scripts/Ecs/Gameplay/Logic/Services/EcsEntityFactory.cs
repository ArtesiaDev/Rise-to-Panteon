using System.Collections.Generic;
using RuntimeRoguelike.Configs;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class EcsEntityFactory
    {
        private readonly PlayerConfig _playerConfig;
        private readonly PlayerAttackConfig _playerAttackConfig;
        private readonly EnemyConfig _enemyConfig;

        public EcsEntityFactory(PlayerConfig playerConfig, PlayerAttackConfig playerAttackConfig, EnemyConfig enemyConfig)
        {
            _playerConfig = playerConfig;
            _playerAttackConfig = playerAttackConfig;
            _enemyConfig = enemyConfig;
        }

        public EcsEntity CreatePlayer(EcsWorld world, Int2 startCell)
        {
            var entity = world.CreateEntity();
            var id = entity.Id;

            world.GetPool<PlayerTag>().Add(id);
            world.GetPool<GridPosition>().Add(id).Value = startCell;
            world.GetPool<RenderPosition>().Add(id).Value = new Float2(startCell.X, startCell.Y);
            world.GetPool<MoveSpeed>().Add(id).CellsPerSecond = _playerConfig.MoveSpeed;
            world.GetPool<MoveCooldown>().Add(id).Remaining = 0f;
            world.GetPool<LastMoveDirection>().Add(id).Value = Int2.Right;

            ref var health = ref world.GetPool<HealthComponent>().Add(id);
            health.Max = _playerConfig.MaxHealth;
            health.Current = _playerConfig.MaxHealth;

            world.GetPool<FactionComponent>().Add(id).Value = Faction.Player;

            ref var stats = ref world.GetPool<PlayerStatsComponent>().Add(id);
            stats.Level = 1;
            stats.XpToNext = 0;
            stats.MoveSpeedMultiplier = 1f;
            stats.BonusDamage = 0;
            stats.Gold = 0;
            stats.Xp = 0;

            world.GetPool<DamageComponent>().Add(id).Value = _playerAttackConfig.BaseDamage;
            world.GetPool<AttackCooldown>().Add(id) = new AttackCooldown
            {
                Remaining = 0f,
                Interval = _playerAttackConfig.AttackCooldown
            };
            world.GetPool<AttackRange>().Add(id).Value = _playerAttackConfig.AttackRange;
            world.GetPool<HazardState>().Add(id) = new HazardState { Current = HazardType.None, SpikeTickRemaining = 0f };

            world.GetPool<SpriteKeyComponent>().Add(id).Value = SpriteKey.Player;
            world.GetPool<NeedsViewTag>().Add(id);

            return entity;
        }

        public EcsEntity CreateEnemy(EcsWorld world, Int2 cell, float difficultyMultiplier, int damageBonus)
        {
            var entity = world.CreateEntity();
            var id = entity.Id;

            world.GetPool<EnemyTag>().Add(id);
            world.GetPool<GridPosition>().Add(id).Value = cell;
            world.GetPool<RenderPosition>().Add(id).Value = new Float2(cell.X, cell.Y);
            world.GetPool<MoveSpeed>().Add(id).CellsPerSecond = _enemyConfig.MoveSpeed;
            world.GetPool<MoveCooldown>().Add(id).Remaining = 0f;

            var maxHp = (int)System.Math.Round(_enemyConfig.MaxHealth * difficultyMultiplier);
            ref var health = ref world.GetPool<HealthComponent>().Add(id);
            health.Max = System.Math.Max(1, maxHp);
            health.Current = health.Max;

            world.GetPool<FactionComponent>().Add(id).Value = Faction.Enemy;
            world.GetPool<DamageComponent>().Add(id).Value = _enemyConfig.BaseDamage + damageBonus;
            world.GetPool<AggroRange>().Add(id).Value = _enemyConfig.AggroRange;
            world.GetPool<AttackRange>().Add(id).Value = _enemyConfig.AttackRange;
            world.GetPool<AttackCooldown>().Add(id) = new AttackCooldown
            {
                Remaining = 0f,
                Interval = _enemyConfig.AttackCooldown
            };

            world.GetPool<PathRefreshCooldown>().Add(id) = new PathRefreshCooldown
            {
                Remaining = 0f,
                Interval = _enemyConfig.PathRefreshInterval
            };

            world.GetPool<IdleMoveCooldown>().Add(id) = new IdleMoveCooldown
            {
                Remaining = _enemyConfig.IdleMoveInterval,
                Interval = _enemyConfig.IdleMoveInterval
            };

            world.GetPool<EnemyPath>().Add(id) = new EnemyPath
            {
                Steps = new List<Int2>(),
                Index = 0
            };

            world.GetPool<TargetEntity>().Add(id) = new TargetEntity
            {
                EntityId = -1,
                HasTarget = false
            };

            world.GetPool<SpriteKeyComponent>().Add(id).Value = SpriteKey.Enemy;
            world.GetPool<NeedsViewTag>().Add(id);

            return entity;
        }

        public EcsEntity CreateLoot(EcsWorld world, Int2 cell, PickupType type, int amount)
        {
            var entity = world.CreateEntity();
            var id = entity.Id;

            world.GetPool<LootTag>().Add(id);
            world.GetPool<GridPosition>().Add(id).Value = cell;
            world.GetPool<RenderPosition>().Add(id).Value = new Float2(cell.X, cell.Y);
            world.GetPool<LootPickup>().Add(id) = new LootPickup
            {
                Type = type,
                Amount = amount
            };

            world.GetPool<SpriteKeyComponent>().Add(id).Value = SpriteKey.Loot;
            world.GetPool<NeedsViewTag>().Add(id);

            return entity;
        }
    }
}
