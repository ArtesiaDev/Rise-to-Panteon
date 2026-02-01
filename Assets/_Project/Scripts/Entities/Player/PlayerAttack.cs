using RuntimeRoguelike.Configs;
using UnityEngine;
using Zenject;

namespace RuntimeRoguelike
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAttack : MonoBehaviour
    {
        private readonly Collider2D[] _hits = new Collider2D[12];

        private PlayerAttackConfig _config;
        private PlayerController _controller;
        private PlayerStats _stats;
        private float _nextAttackTime;

        [Inject]
        private void Construct(PlayerAttackConfig config)
        {
            _config = config;
        }

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_config.AttackKey))
            {
                TryAttack();
            }
        }

        public void Initialize(PlayerStats playerStats)
        {
            _stats = playerStats;
        }

        private void TryAttack()
        {
            if (Time.time < _nextAttackTime)
            {
                return;
            }

            _nextAttackTime = Time.time + _config.AttackCooldown;
            var direction = _controller != null ? _controller.LastMoveDirection : Vector2Int.right;
            var origin = (Vector2)transform.position + new Vector2(direction.x, direction.y) * _config.AttackOffset;
            var count = Physics2D.OverlapCircleNonAlloc(origin, _config.AttackRange, _hits);
            var damage = _config.BaseDamage + (_stats != null ? _stats.BonusDamage : 0);

            for (var i = 0; i < count; i++)
            {
                var health = _hits[i].GetComponentInParent<Health>();
                if (health != null && health.Faction == Faction.Enemy)
                {
                    health.TakeDamage(damage);
                }
            }
        }
    }
}
