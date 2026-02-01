using System;
using UnityEngine;

namespace RuntimeRoguelike
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private int gold;
        [SerializeField] private int xp;
        [SerializeField] private int level = 1;
        [SerializeField] private int xpToNextLevel = 10;
        [SerializeField] private float moveSpeedMultiplier = 1f;
        [SerializeField] private int bonusDamage;

        public int Gold => gold;
        public int Xp => xp;
        public int Level => level;
        public int XpToNextLevel => xpToNextLevel;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public int BonusDamage => bonusDamage;

        public event Action<int> OnGoldChanged;
        public event Action<int, int> OnXpChanged;
        public event Action<int> OnLevelChanged;

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            gold += amount;
            OnGoldChanged?.Invoke(gold);
        }

        public void AddXp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            xp += amount;
            OnXpChanged?.Invoke(xp, xpToNextLevel);
        }

        public void ConsumeXp(int amount)
        {
            ConsumeXp(amount, true);
        }

        public void ConsumeXp(int amount, bool notify)
        {
            xp = Mathf.Max(0, xp - amount);
            if (notify)
            {
                OnXpChanged?.Invoke(xp, xpToNextLevel);
            }
        }

        public void NotifyXpChanged()
        {
            OnXpChanged?.Invoke(xp, xpToNextLevel);
        }

        public void LevelUp()
        {
            level += 1;
            OnLevelChanged?.Invoke(level);
        }

        public void SetXpToNextLevel(int newValue)
        {
            SetXpToNextLevel(newValue, true);
        }

        public void SetXpToNextLevel(int newValue, bool notify)
        {
            xpToNextLevel = Mathf.Max(1, newValue);
            if (notify)
            {
                OnXpChanged?.Invoke(xp, xpToNextLevel);
            }
        }

        public void AddMoveSpeedMultiplier(float amount)
        {
            moveSpeedMultiplier = Mathf.Max(0.1f, moveSpeedMultiplier + amount);
        }

        public void AddBonusDamage(int amount)
        {
            bonusDamage += amount;
        }
    }
}
