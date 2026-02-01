using System;
using UnityEngine;

namespace RuntimeRoguelike
{
    public class Health : MonoBehaviour
    {
        public event Action<int, int> OnDamaged;
        public event Action OnDied;

        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public Faction Faction { get; private set; }

        public void Initialize(int maxHp, Faction faction)
        {
            MaxHp = Mathf.Max(1, maxHp);
            CurrentHp = MaxHp;
            Faction = faction;
            OnDamaged?.Invoke(CurrentHp, MaxHp);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHp <= 0)
            {
                return;
            }

            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnDamaged?.Invoke(CurrentHp, MaxHp);

            if (CurrentHp <= 0)
            {
                OnDied?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHp <= 0)
            {
                return;
            }

            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            OnDamaged?.Invoke(CurrentHp, MaxHp);
        }

        public void AddMaxHp(int amount, bool healToMax)
        {
            if (amount <= 0)
            {
                return;
            }

            MaxHp += amount;
            if (healToMax)
            {
                CurrentHp = MaxHp;
            }
            else
            {
                CurrentHp = Mathf.Min(CurrentHp, MaxHp);
            }

            OnDamaged?.Invoke(CurrentHp, MaxHp);
        }
    }
}
