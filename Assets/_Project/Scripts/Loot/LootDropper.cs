using RuntimeRoguelike.Configs;
using UnityEngine;
using Zenject;

namespace RuntimeRoguelike
{
    public class LootDropper : MonoBehaviour
    {
        private SpriteFactory _spriteFactory;
        private LootConfig _lootConfig;
        private Health _health;

        private System.Random _rng;
        private Transform _parent;
        private RunStats _runStats;

        [Inject]
        private void Construct(SpriteFactory spriteFactory, LootConfig lootConfig)
        {
            _spriteFactory = spriteFactory;
            _lootConfig = lootConfig;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        public void Initialize(System.Random rng, Transform lootParent, RunStats stats, LootConfig overrideConfig)
        {
            _rng = rng;
            _parent = lootParent;
            _runStats = stats;
            if (overrideConfig != null)
            {
                _lootConfig = overrideConfig;
            }

            if (_health == null)
            {
                _health = GetComponent<Health>();
            }

            if (_health != null)
            {
                _health.OnDied += TryDrop;
            }
        }

        private void TryDrop()
        {
            if (_rng == null || _parent == null || _lootConfig == null || _lootConfig.LootTable == null || _lootConfig.LootTable.Length == 0)
            {
                return;
            }

            if (_rng.NextDouble() > _lootConfig.DropChance)
            {
                return;
            }

            var entry = PickEntry();
            var pickupObject = new GameObject("Pickup");
            pickupObject.transform.SetParent(_parent, false);
            pickupObject.transform.position = transform.position;

            var renderer = pickupObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _spriteFactory.GetSprite(SpriteKey.Loot);

            var collider = pickupObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;

            var pickup = pickupObject.AddComponent<Pickup>();
            pickup.Initialize(entry.Type, entry.Amount, _runStats);
            _runStats?.IncrementLoot();
        }

        private LootEntry PickEntry()
        {
            var totalWeight = 0;
            for (var i = 0; i < _lootConfig.LootTable.Length; i++)
            {
                totalWeight += _lootConfig.LootTable[i].Weight;
            }

            var roll = _rng.Next(0, Mathf.Max(1, totalWeight));
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
