using UnityEngine;

namespace RuntimeRoguelike
{
    public class RunStats
    {
        public int Seed { get; private set; }
        public Vector2Int PlayerCell { get; private set; }
        public int EnemyCount { get; private set; }
        public int LootCount { get; private set; }

        public void Reset(int seed)
        {
            Seed = seed;
            PlayerCell = Vector2Int.zero;
            EnemyCount = 0;
            LootCount = 0;
        }

        public void SetPlayerCell(Vector2Int cell)
        {
            PlayerCell = cell;
        }

        public void IncrementEnemy()
        {
            EnemyCount += 1;
        }

        public void DecrementEnemy()
        {
            EnemyCount = Mathf.Max(0, EnemyCount - 1);
        }

        public void IncrementLoot()
        {
            LootCount += 1;
        }

        public void DecrementLoot()
        {
            LootCount = Mathf.Max(0, LootCount - 1);
        }
    }
}
