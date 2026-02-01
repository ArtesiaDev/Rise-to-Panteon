using RuntimeRoguelike.Configs;
using UnityEngine;
using Zenject;

namespace RuntimeRoguelike
{
    public class EnemyFactory
    {
        private readonly DiContainer _container;
        private readonly SpriteFactory _spriteFactory;
        private readonly GridPositionConverter _gridPositionConverter;

        public EnemyFactory(DiContainer container, SpriteFactory spriteFactory, GridPositionConverter gridPositionConverter)
        {
            _container = container;
            _spriteFactory = spriteFactory;
            _gridPositionConverter = gridPositionConverter;
        }

        public Health CreateEnemy(
            Vector2Int cell,
            Transform parent,
            MapGrid mapGrid,
            GridOccupancy occupancy,
            AStarPathfinder pathfinder,
            PlayerController player,
            EnemyConfig enemyConfig,
            DifficultyService difficulty,
            LootConfig lootConfig,
            RunStats runStats,
            System.Random rng)
        {
            var enemyObject = new GameObject("Enemy");
            enemyObject.transform.SetParent(parent, false);
            enemyObject.transform.position = _gridPositionConverter.CellToWorld(cell);

            var renderer = enemyObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _spriteFactory.GetSprite(SpriteKey.Enemy);

            var body = enemyObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            var collider = enemyObject.AddComponent<BoxCollider2D>();
            collider.size = enemyConfig.ColliderSize;

            var health = _container.InstantiateComponent<Health>(enemyObject);
            var playerStats = player != null ? player.GetComponent<PlayerStats>() : null;
            var difficultyMultiplier = difficulty != null ? difficulty.GetEnemyStatMultiplier(playerStats) : 1f;
            var maxHp = Mathf.RoundToInt(enemyConfig.MaxHealth * difficultyMultiplier);
            health.Initialize(maxHp, Faction.Enemy);

            var ai = _container.InstantiateComponent<EnemyAI>(enemyObject);
            var damageBonus = Mathf.RoundToInt((difficultyMultiplier - 1f) * 2f);
            ai.Initialize(mapGrid, occupancy, pathfinder, player, cell, rng.Next(), damageBonus);

            var dropper = _container.InstantiateComponent<LootDropper>(enemyObject);
            dropper.Initialize(rng, parent, runStats, lootConfig);

            return health;
        }
    }
}
