using UnityEngine;

namespace RuntimeRoguelike
{
    public sealed class HazardGenerator
    {
        private readonly System.Random rng;
        private readonly float hazardChance;

        public HazardGenerator(int seed, float hazardChance)
        {
            rng = new System.Random(seed + 4444);
            this.hazardChance = hazardChance;
        }

        public void Populate(MapGrid grid, Vector2Int safeCenter, int safeRadius)
        {
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

                    if (rng.NextDouble() > hazardChance)
                    {
                        continue;
                    }

                    var type = rng.NextDouble() < 0.6 ? HazardType.Spike : HazardType.Poison;
                    grid.SetHazard(cell, type);
                }
            }
        }
    }
}
