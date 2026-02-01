using RuntimeRoguelike.Configs;
using UnityEngine;
using Zenject;

namespace RuntimeRoguelike
{
    public class HazardSystem
    {
        private readonly HazardConfig _hazardConfig;

        private MapGrid _grid;
        private PlayerController _player;
        private Health _health;
        private StatusEffects _statusEffects;
        private HazardType _currentHazard = HazardType.None;
        private float _nextSpikeTick;

        [Inject]
        public HazardSystem(HazardConfig hazardConfig)
        {
            _hazardConfig = hazardConfig;
        }

        public void Initialize(MapGrid mapGrid, PlayerController playerController)
        {
            _grid = mapGrid;
            _player = playerController;
            _health = playerController.GetComponent<Health>();
            _statusEffects = playerController.GetComponent<StatusEffects>();
            _player.OnCellChanged += HandleCellChanged;
        }

        public void Tick()
        {
            if (_currentHazard != HazardType.Spike || _health == null)
            {
                return;
            }

            if (Time.time >= _nextSpikeTick)
            {
                _nextSpikeTick = Time.time + _hazardConfig.SpikeTickInterval;
                _health.TakeDamage(_hazardConfig.SpikeDamage);
            }
        }

        private void HandleCellChanged(Vector2Int cell)
        {
            var hazard = _grid.Get(cell).Hazard;
            _currentHazard = hazard;
            if (hazard == HazardType.Spike)
            {
                _nextSpikeTick = Time.time + _hazardConfig.SpikeTickInterval;
                _health?.TakeDamage(_hazardConfig.SpikeDamage);
            }
            else if (hazard == HazardType.Poison)
            {
                _statusEffects?.ApplyPoison(_hazardConfig.PoisonDuration, _hazardConfig.PoisonDps, _hazardConfig.PoisonTickInterval);
            }
        }
    }
}
