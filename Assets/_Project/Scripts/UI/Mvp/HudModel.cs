using System;

namespace RuntimeRoguelike.UI.Mvp
{
    public class HudModel
    {
        public event Action OnChanged;

        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public int Xp { get; private set; }
        public int XpToNext { get; private set; }
        public int Level { get; private set; }
        public int Gold { get; private set; }
        public int Seed { get; private set; }

        public void SetHp(int current, int max)
        {
            if (CurrentHp == current && MaxHp == max)
            {
                return;
            }

            CurrentHp = current;
            MaxHp = max;
            OnChanged?.Invoke();
        }

        public void SetProgress(int xp, int xpToNext, int level)
        {
            if (Xp == xp && XpToNext == xpToNext && Level == level)
            {
                return;
            }

            Xp = xp;
            XpToNext = xpToNext;
            Level = level;
            OnChanged?.Invoke();
        }

        public void SetGold(int gold)
        {
            if (Gold == gold)
            {
                return;
            }

            Gold = gold;
            OnChanged?.Invoke();
        }

        public void SetSeed(int seed)
        {
            if (Seed == seed)
            {
                return;
            }

            Seed = seed;
            OnChanged?.Invoke();
        }
    }
}
