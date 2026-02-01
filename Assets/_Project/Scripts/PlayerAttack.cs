using UnityEngine;

namespace RuntimeRoguelike
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private int baseDamage = 2;
        [SerializeField] private float attackCooldown = 0.4f;
        [SerializeField] private float attackRange = 0.7f;
        [SerializeField] private float attackOffset = 0.6f;
        [SerializeField] private KeyCode attackKey = KeyCode.Space;

        private readonly Collider2D[] hits = new Collider2D[12];
        private float nextAttackTime;
        private PlayerController controller;
        private PlayerStats stats;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        public void Initialize(PlayerStats playerStats)
        {
            stats = playerStats;
        }

        private void Update()
        {
            if (Input.GetKeyDown(attackKey))
            {
                TryAttack();
            }
        }

        private void TryAttack()
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            var direction = controller != null ? controller.LastMoveDirection : Vector2Int.right;
            var origin = (Vector2)transform.position + new Vector2(direction.x, direction.y) * attackOffset;
            var count = Physics2D.OverlapCircleNonAlloc(origin, attackRange, hits);
            var damage = baseDamage + (stats != null ? stats.BonusDamage : 0);

            for (var i = 0; i < count; i++)
            {
                var health = hits[i].GetComponentInParent<Health>();
                if (health != null && health.Faction == Faction.Enemy)
                {
                    health.TakeDamage(damage);
                }
            }
        }
    }
}
