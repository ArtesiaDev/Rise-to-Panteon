using RuntimeRoguelike.Configs;
using UnityEngine;

namespace RuntimeRoguelike
{
    public class DifficultyService
    {
        private DifficultyConfig _config;
        private float _startTime;

        public void Configure(DifficultyConfig config)
        {
            _config = config;
        }

        public void Initialize()
        {
            _startTime = Time.time;
        }

        public float GetEnemyStatMultiplier(PlayerStats stats)
        {
            if (_config == null)
            {
                return 1f;
            }

            var timeFactor = Mathf.Clamp01((Time.time - _startTime) / Mathf.Max(1f, _config.TimeToMaxDifficulty));
            var baseMultiplier = Mathf.Lerp(1f, _config.MaxEnemyStatMultiplier, timeFactor);
            if (stats == null)
            {
                return baseMultiplier;
            }

            var levelBonus = 1f + Mathf.Max(0, stats.Level - 1) * _config.LevelStatBonus;
            return baseMultiplier * levelBonus;
        }

        public float GetSpawnRateMultiplier(PlayerStats stats)
        {
            if (_config == null)
            {
                return 1f;
            }

            var timeFactor = Mathf.Clamp01((Time.time - _startTime) / Mathf.Max(1f, _config.TimeToMaxDifficulty));
            var baseMultiplier = Mathf.Lerp(1f, _config.MaxSpawnRateMultiplier, timeFactor);
            if (stats == null)
            {
                return baseMultiplier;
            }

            var levelBonus = 1f + Mathf.Max(0, stats.Level - 1) * (_config.LevelStatBonus * 0.5f);
            return baseMultiplier * levelBonus;
        }
    }
}
