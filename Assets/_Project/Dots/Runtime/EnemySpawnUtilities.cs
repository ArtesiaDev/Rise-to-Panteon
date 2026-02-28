using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    /// <summary>
    /// Общая логика инициализации врага, используется в SpawnInitialEnemiesSystem и EnemySpawnerSystem.
    /// </summary>
    public static class EnemySpawnUtilities
    {
        public static void InitializeEnemy(EntityManager em, Entity entity, int2 cell, EnemyConfigData config, float difficultyMultiplier, int damageBonus)
        {
            EntityUtilities.EnsureComponent(em, entity, new EnemyTag());
            EntityUtilities.EnsureComponent(em, entity, new RunTag());
            EntityUtilities.EnsureComponent(em, entity, new SpriteKeyComponent { Value = DotsSpriteKey.Enemy });
            EntityUtilities.EnsureComponent(em, entity, new GridPosition { Value = cell });
            EntityUtilities.EnsureComponent(em, entity, new PreviousGridPosition { Value = cell });
            EntityUtilities.EnsureComponent(em, entity, new RenderPosition { Value = new float2(cell.x, cell.y) });
            EntityUtilities.EnsureComponent(em, entity, new MoveSpeed { CellsPerSecond = config.MoveSpeed });
            EntityUtilities.EnsureComponent(em, entity, new MoveCooldown { Remaining = 0f });

            var maxHp = math.max(1, (int)math.round(config.MaxHealth * difficultyMultiplier));
            EntityUtilities.EnsureComponent(em, entity, new Health { Max = maxHp, Current = maxHp });
            EntityUtilities.EnsureComponent(em, entity, new Damage { Value = config.BaseDamage + damageBonus });
            EntityUtilities.EnsureComponent(em, entity, new AggroRange { Value = config.AggroRange });
            EntityUtilities.EnsureComponent(em, entity, new AttackRange { Value = config.AttackRange });
            EntityUtilities.EnsureComponent(em, entity, new AttackCooldown { Remaining = 0f, Interval = config.AttackCooldown });
            EntityUtilities.EnsureComponent(em, entity, new PathRefreshCooldown { Remaining = 0f, Interval = config.PathRefreshInterval });
            EntityUtilities.EnsureComponent(em, entity, new IdleMoveCooldown { Remaining = config.IdleMoveInterval, Interval = config.IdleMoveInterval });
            EntityUtilities.EnsureComponent(em, entity, new PathIndex { Value = 0 });
            EntityUtilities.EnsureComponent(em, entity, new Target { Value = Entity.Null, HasTarget = false });
            EntityUtilities.EnsureComponent(em, entity, new HazardState { Current = HazardType.None, SpikeTickRemaining = 0f });

            if (!em.HasComponent<PathStep>(entity))
            {
                em.AddBuffer<PathStep>(entity);
            }
            else
            {
                em.GetBuffer<PathStep>(entity).Clear();
            }

            EntityUtilities.EnsureComponent(em, entity, new MoveIntent { Direction = int2.zero });
            em.SetComponentEnabled<MoveIntent>(entity, false);

            EntityUtilities.EnsureComponent(em, entity, new AttackRequest());
            em.SetComponentEnabled<AttackRequest>(entity, false);
        }
    }
}
