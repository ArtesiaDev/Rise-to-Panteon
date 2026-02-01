using UnityEngine;

namespace RuntimeRoguelike
{
    public class DifficultyDirector : MonoBehaviour
    {
        [SerializeField] private float timeToMaxDifficulty = 300f;
        [SerializeField] private float maxEnemyStatMultiplier = 2f;
        [SerializeField] private float maxSpawnRateMultiplier = 2f;
        [SerializeField] private float levelStatBonus = 0.05f;

        private float startTime;

        public void Configure(float timeToMax, float maxStatMultiplier, float maxSpawnMultiplier, float levelBonus)
        {
            timeToMaxDifficulty = Mathf.Max(10f, timeToMax);
            maxEnemyStatMultiplier = Mathf.Max(1f, maxStatMultiplier);
            maxSpawnRateMultiplier = Mathf.Max(1f, maxSpawnMultiplier);
            levelStatBonus = Mathf.Max(0f, levelBonus);
        }

        public void Initialize()
        {
            startTime = Time.time;
        }

        public float GetEnemyStatMultiplier(PlayerStats stats)
        {
            var timeFactor = Mathf.Clamp01((Time.time - startTime) / Mathf.Max(1f, timeToMaxDifficulty));
            var baseMultiplier = Mathf.Lerp(1f, maxEnemyStatMultiplier, timeFactor);
            if (stats == null)
            {
                return baseMultiplier;
            }

            var levelBonus = 1f + Mathf.Max(0, stats.Level - 1) * levelStatBonus;
            return baseMultiplier * levelBonus;
        }

        public float GetSpawnRateMultiplier(PlayerStats stats)
        {
            var timeFactor = Mathf.Clamp01((Time.time - startTime) / Mathf.Max(1f, timeToMaxDifficulty));
            var baseMultiplier = Mathf.Lerp(1f, maxSpawnRateMultiplier, timeFactor);
            if (stats == null)
            {
                return baseMultiplier;
            }

            var levelBonus = 1f + Mathf.Max(0, stats.Level - 1) * (levelStatBonus * 0.5f);
            return baseMultiplier * levelBonus;
        }
    }
}
