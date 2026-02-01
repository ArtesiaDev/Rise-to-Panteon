using UnityEngine;

namespace RuntimeRoguelike
{
    public enum PickupType
    {
        Gold,
        Xp,
        Heal
    }

    [RequireComponent(typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupType pickupType = PickupType.Gold;
        [SerializeField] private int amount = 1;

        private RunStats runStats;

        public void Initialize(PickupType type, int value, RunStats stats)
        {
            pickupType = type;
            amount = value;
            runStats = stats;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var playerStats = other.GetComponentInParent<PlayerStats>();
            if (playerStats == null)
            {
                return;
            }

            switch (pickupType)
            {
                case PickupType.Gold:
                    playerStats.AddGold(amount);
                    break;
                case PickupType.Xp:
                    playerStats.AddXp(amount);
                    break;
                case PickupType.Heal:
                    var health = other.GetComponentInParent<Health>();
                    if (health != null)
                    {
                        health.Heal(amount);
                    }
                    break;
            }

            runStats?.DecrementLoot();
            Destroy(gameObject);
        }
    }
}
