using System;
using UnityEngine;

namespace RuntimeRoguelike
{
    public enum Faction
    {
        Player,
        Enemy,
        Neutral
    }

    public class Health : MonoBehaviour
    {
        [SerializeField] private Faction faction = Faction.Neutral;
        [SerializeField] private int maxHp = 10;
        [SerializeField] private int currentHp = 10;

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public Faction Faction => faction;

        public event Action<int, int> OnDamaged;
        public event Action OnDied;

        private void Awake()
        {
            if (currentHp <= 0)
            {
                currentHp = maxHp;
            }
        }

        public void Initialize(int maxHpValue, Faction newFaction)
        {
            maxHp = Mathf.Max(1, maxHpValue);
            currentHp = maxHp;
            faction = newFaction;
            OnDamaged?.Invoke(currentHp, maxHp);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || currentHp <= 0)
            {
                return;
            }

            currentHp = Mathf.Max(0, currentHp - amount);
            OnDamaged?.Invoke(currentHp, maxHp);

            if (currentHp <= 0)
            {
                OnDied?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || currentHp <= 0)
            {
                return;
            }

            currentHp = Mathf.Min(maxHp, currentHp + amount);
            OnDamaged?.Invoke(currentHp, maxHp);
        }

        public void AddMaxHp(int amount, bool healToMax)
        {
            if (amount <= 0)
            {
                return;
            }

            maxHp += amount;
            if (healToMax)
            {
                currentHp = maxHp;
            }
            else
            {
                currentHp = Mathf.Min(currentHp, maxHp);
            }

            OnDamaged?.Invoke(currentHp, maxHp);
        }
    }
}
