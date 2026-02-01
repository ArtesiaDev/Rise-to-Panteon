using RuntimeRoguelike.Configs;
using UnityEngine;

namespace RuntimeRoguelike
{
    public class LevelSystem
    {
        private PlayerStats _stats;
        private Health _health;
        private PerkSelectionUI _perkUi;
        private PerkConfig _perkConfig;
        private LevelConfig _levelConfig;
        private System.Random _rng;

        public void Initialize(PlayerStats playerStats, Health playerHealth, PerkSelectionUI perkUi, PerkConfig perkConfig, LevelConfig levelConfig, int seed)
        {
            _stats = playerStats;
            _health = playerHealth;
            _perkUi = perkUi;
            _perkConfig = perkConfig;
            _levelConfig = levelConfig;
            _rng = new System.Random(seed + 2222);

            _stats.SetXpToNextLevel(GetXpForLevel(_stats.Level), false);
            _stats.OnXpChanged += HandleXpChanged;
            _stats.NotifyXpChanged();
        }

        private void HandleXpChanged(int xp, int xpToNext)
        {
            var leveled = false;
            while (_stats.Xp >= _stats.XpToNextLevel)
            {
                _stats.ConsumeXp(_stats.XpToNextLevel, false);
                _stats.LevelUp();
                _stats.SetXpToNextLevel(GetXpForLevel(_stats.Level), false);
                ShowPerkSelection();
                leveled = true;
            }

            if (leveled)
            {
                _stats.NotifyXpChanged();
            }
        }

        private void ShowPerkSelection()
        {
            var perks = PickRandomPerks(_perkConfig.Perks, _perkConfig.ChoicesCount);
            _perkUi.Show(perks, ApplyPerk);
        }

        private void ApplyPerk(PerkDefinition perk)
        {
            switch (perk.Type)
            {
                case PerkType.MaxHp:
                    _health?.AddMaxHp(Mathf.RoundToInt(perk.Value), true);
                    break;
                case PerkType.Damage:
                    _stats?.AddBonusDamage(Mathf.RoundToInt(perk.Value));
                    break;
                case PerkType.MoveSpeed:
                    _stats?.AddMoveSpeedMultiplier(perk.Value);
                    break;
            }

            _perkUi.Hide();
        }

        private PerkDefinition[] PickRandomPerks(PerkDefinition[] source, int count)
        {
            if (source == null || source.Length == 0)
            {
                return new PerkDefinition[0];
            }

            count = Mathf.Clamp(count, 1, source.Length);
            var indices = new int[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                indices[i] = i;
            }

            for (var i = 0; i < source.Length; i++)
            {
                var swap = _rng.Next(i, source.Length);
                var temp = indices[i];
                indices[i] = indices[swap];
                indices[swap] = temp;
            }

            var result = new PerkDefinition[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = source[indices[i]];
            }

            return result;
        }

        private int GetXpForLevel(int level)
        {
            return _levelConfig.BaseXpToLevel + Mathf.Max(0, level - 1) * _levelConfig.XpIncreasePerLevel;
        }
    }
}
