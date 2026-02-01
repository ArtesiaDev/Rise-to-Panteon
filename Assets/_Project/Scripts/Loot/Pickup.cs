using UnityEngine;

namespace RuntimeRoguelike
{
    [RequireComponent(typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        private PickupType _pickupType;
        private int _amount;
        private RunStats _runStats;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var playerStats = other.GetComponentInParent<PlayerStats>();
            if (playerStats == null)
            {
                return;
            }

            switch (_pickupType)
            {
                case PickupType.Gold:
                    playerStats.AddGold(_amount);
                    break;
                case PickupType.Xp:
                    playerStats.AddXp(_amount);
                    break;
                case PickupType.Heal:
                    var health = other.GetComponentInParent<Health>();
                    if (health != null)
                    {
                        health.Heal(_amount);
                    }
                    break;
            }

            _runStats?.DecrementLoot();
            Destroy(gameObject);
        }

        public void Initialize(PickupType type, int value, RunStats stats)
        {
            _pickupType = type;
            _amount = value;
            _runStats = stats;
        }
    }
}
