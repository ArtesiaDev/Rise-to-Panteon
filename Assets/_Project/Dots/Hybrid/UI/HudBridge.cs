using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class HudBridge : MonoBehaviour
    {
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _xpText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _goldText;
        [SerializeField] private Text _seedText;

        private World _world;
        private EntityManager _entityManager;
        private EntityQuery _playerQuery;
        private EntityQuery _runStateQuery;

        private void Awake()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = _world.EntityManager;
            _playerQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<PlayerStats>(),
                ComponentType.ReadOnly<Health>());
            _runStateQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RunState>());
        }
        
        private void Update()
        {
            if (_world is not { IsCreated: true })
                return;
            
            if (_playerQuery.IsEmpty)
                return;
            
            var playerEntity = _playerQuery.GetSingletonEntity();
            var stats = _entityManager.GetComponentData<PlayerStats>(playerEntity);
            var health = _entityManager.GetComponentData<Health>(playerEntity);

            if (_hpText != null)
            {
                _hpText.text = $"HP: {health.Current}/{health.Max}";
            }

            if (_xpText != null)
            {
                _xpText.text = $"XP: {stats.Xp}/{stats.XpToNext}";
            }

            if (_levelText != null)
            {
                _levelText.text = $"Level: {stats.Level}";
            }

            if (_goldText != null)
            {
                _goldText.text = $"Gold: {stats.Gold}";
            }

            if (_seedText != null && _runStateQuery.TryGetSingleton<RunState>(out var runState))
            {
                _seedText.text = $"Seed: {runState.Seed}";
            }
        }
    }
}
