using System;
using UnityEngine;

namespace RuntimeRoguelike
{
    public class PlayerStats : MonoBehaviour
    {
        public event Action<int> OnGoldChanged;
        public event Action<int, int> OnXpChanged;
        public event Action<int> OnLevelChanged;

        public int Gold { get; private set; }
        public int Xp { get; private set; }
        public int Level { get; private set; } = 1;
        public int XpToNextLevel { get; private set; } = 10;
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public int BonusDamage { get; private set; }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public void AddXp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Xp += amount;
            OnXpChanged?.Invoke(Xp, XpToNextLevel);
        }

        public void ConsumeXp(int amount)
        {
            ConsumeXp(amount, true);
        }

        public void ConsumeXp(int amount, bool notify)
        {
            Xp = Mathf.Max(0, Xp - amount);
            if (notify)
            {
                OnXpChanged?.Invoke(Xp, XpToNextLevel);
            }
        }

        public void NotifyXpChanged()
        {
            OnXpChanged?.Invoke(Xp, XpToNextLevel);
        }

        public void LevelUp()
        {
            Level += 1;
            OnLevelChanged?.Invoke(Level);
        }

        public void SetXpToNextLevel(int newValue)
        {
            SetXpToNextLevel(newValue, true);
        }

        public void SetXpToNextLevel(int newValue, bool notify)
        {
            XpToNextLevel = Mathf.Max(1, newValue);
            if (notify)
            {
                OnXpChanged?.Invoke(Xp, XpToNextLevel);
            }
        }

        public void AddMoveSpeedMultiplier(float amount)
        {
            MoveSpeedMultiplier = Mathf.Max(0.1f, MoveSpeedMultiplier + amount);
        }

        public void AddBonusDamage(int amount)
        {
            BonusDamage += amount;
        }
    }
}
