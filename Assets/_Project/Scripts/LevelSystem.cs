using UnityEngine;

namespace RuntimeRoguelike
{
    public class LevelSystem : MonoBehaviour
    {
        [SerializeField] private int baseXpToLevel = 10;
        [SerializeField] private int xpIncreasePerLevel = 5;

        private PlayerStats stats;
        private Health health;
        private PerkSelectionUI perkUI;
        private System.Random rng;

        public void Initialize(PlayerStats playerStats, Health playerHealth, PerkSelectionUI selectionUI, int seed)
        {
            stats = playerStats;
            health = playerHealth;
            perkUI = selectionUI;
            rng = new System.Random(seed + 2222);
            stats.SetXpToNextLevel(GetXpForLevel(stats.Level));
            stats.OnXpChanged += HandleXpChanged;
        }

        private void HandleXpChanged(int xp, int xpToNext)
        {
            var leveled = false;
            while (stats.Xp >= stats.XpToNextLevel)
            {
                stats.ConsumeXp(stats.XpToNextLevel, false);
                stats.LevelUp();
                stats.SetXpToNextLevel(GetXpForLevel(stats.Level), false);
                ShowPerkSelection();
                leveled = true;
            }

            if (leveled)
            {
                stats.NotifyXpChanged();
            }
        }

        private void ShowPerkSelection()
        {
            var perks = PerkLibrary.PickRandom(3, rng);
            perkUI.Show(perks, ApplyPerk);
        }

        private void ApplyPerk(PerkDefinition perk)
        {
            switch (perk.Type)
            {
                case PerkType.MaxHp:
                    health?.AddMaxHp(Mathf.RoundToInt(perk.Value), true);
                    break;
                case PerkType.Damage:
                    stats?.AddBonusDamage(Mathf.RoundToInt(perk.Value));
                    break;
                case PerkType.MoveSpeed:
                    stats?.AddMoveSpeedMultiplier(perk.Value);
                    break;
            }

            perkUI.Hide();
        }

        private int GetXpForLevel(int level)
        {
            return baseXpToLevel + Mathf.Max(0, level - 1) * xpIncreasePerLevel;
        }
    }
}
