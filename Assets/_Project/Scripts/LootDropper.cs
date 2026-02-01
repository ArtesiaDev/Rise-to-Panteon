using UnityEngine;

namespace RuntimeRoguelike
{
    [System.Serializable]
    public struct LootEntry
    {
        public PickupType type;
        public int amount;
        public int weight;
    }

    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private float dropChance = 0.45f;
        [SerializeField] private LootEntry[] lootTable = new LootEntry[]
        {
            new LootEntry { type = PickupType.Gold, amount = 2, weight = 6 },
            new LootEntry { type = PickupType.Xp, amount = 3, weight = 4 },
            new LootEntry { type = PickupType.Heal, amount = 2, weight = 2 }
        };

        private System.Random rng;
        private Transform parent;
        private RunStats runStats;
        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        public void Initialize(System.Random random, Transform lootParent, RunStats stats)
        {
            rng = random;
            parent = lootParent;
            runStats = stats;

            if (health != null)
            {
                health.OnDied += TryDrop;
            }
        }

        private void TryDrop()
        {
            if (rng == null || parent == null || lootTable.Length == 0)
            {
                return;
            }

            if (rng.NextDouble() > dropChance)
            {
                return;
            }

            var entry = PickEntry();
            var pickupObject = new GameObject("Pickup");
            pickupObject.transform.SetParent(parent, false);
            pickupObject.transform.position = transform.position;

            var renderer = pickupObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Get(SpriteKey.Loot);

            var collider = pickupObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;

            var pickup = pickupObject.AddComponent<Pickup>();
            pickup.Initialize(entry.type, entry.amount, runStats);
            runStats?.IncrementLoot();
        }

        private LootEntry PickEntry()
        {
            var totalWeight = 0;
            for (var i = 0; i < lootTable.Length; i++)
            {
                totalWeight += lootTable[i].weight;
            }

            var roll = rng.Next(0, Mathf.Max(1, totalWeight));
            var cumulative = 0;
            for (var i = 0; i < lootTable.Length; i++)
            {
                cumulative += lootTable[i].weight;
                if (roll < cumulative)
                {
                    return lootTable[i];
                }
            }

            return lootTable[0];
        }
    }
}
