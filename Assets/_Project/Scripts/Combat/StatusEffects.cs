using UnityEngine;

namespace RuntimeRoguelike
{
    public class StatusEffects : MonoBehaviour
    {
        private Health _health;
        private float _poisonRemaining;
        private float _poisonDps;
        private float _poisonTickInterval = 0.5f;
        private float _nextPoisonTick;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void Update()
        {
            if (_poisonRemaining <= 0f || _health == null)
            {
                return;
            }

            _poisonRemaining -= Time.deltaTime;
            if (Time.time >= _nextPoisonTick)
            {
                _nextPoisonTick = Time.time + _poisonTickInterval;
                var damage = Mathf.Max(1, Mathf.RoundToInt(_poisonDps * _poisonTickInterval));
                _health.TakeDamage(damage);
            }
        }

        public void ApplyPoison(float duration, float dps, float tickInterval)
        {
            _poisonRemaining = Mathf.Max(_poisonRemaining, duration);
            _poisonDps = Mathf.Max(_poisonDps, dps);
            _poisonTickInterval = Mathf.Max(0.05f, tickInterval);
            _nextPoisonTick = Time.time + _poisonTickInterval;
        }
    }
}
