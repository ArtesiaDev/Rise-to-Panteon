using RuntimeRoguelike.Configs;
using UnityEngine;

namespace RuntimeRoguelike
{
    public class HazardGenerator
    {
        private readonly HazardConfig _config;

        public HazardGenerator(HazardConfig config)
        {
            _config = config;
        }

        public void Populate(MapGrid grid, Vector2Int safeCenter, int safeRadius, int seed)
        {
            var rng = new System.Random(seed + 4444);
            for (var y = 1; y < grid.Height - 1; y++)
            {
                for (var x = 1; x < grid.Width - 1; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!grid.IsWalkable(cell))
                    {
                        continue;
                    }

                    var dx = x - safeCenter.x;
                    var dy = y - safeCenter.y;
                    if (dx * dx + dy * dy <= safeRadius * safeRadius)
                    {
                        continue;
                    }

                    if (rng.NextDouble() > _config.HazardChance)
                    {
                        continue;
                    }

                    var type = rng.NextDouble() < _config.SpikeChance ? HazardType.Spike : HazardType.Poison;
                    grid.SetHazard(cell, type);
                }
            }
        }
    }
}
