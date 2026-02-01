using UnityEngine;

namespace RuntimeRoguelike
{
    public class StatusEffects : MonoBehaviour
    {
        [SerializeField] private float poisonTickInterval = 0.5f;

        private Health health;
        private float poisonRemaining;
        private float poisonDps;
        private float nextPoisonTick;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        public void ApplyPoison(float duration, float dps)
        {
            poisonRemaining = Mathf.Max(poisonRemaining, duration);
            poisonDps = Mathf.Max(poisonDps, dps);
            nextPoisonTick = Time.time + poisonTickInterval;
        }

        private void Update()
        {
            if (poisonRemaining <= 0f || health == null)
            {
                return;
            }

            poisonRemaining -= Time.deltaTime;
            if (Time.time >= nextPoisonTick)
            {
                nextPoisonTick = Time.time + poisonTickInterval;
                var damage = Mathf.Max(1, Mathf.RoundToInt(poisonDps * poisonTickInterval));
                health.TakeDamage(damage);
            }
        }
    }
}
