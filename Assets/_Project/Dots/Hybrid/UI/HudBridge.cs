using RuntimeRoguelike.Dots;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class HudBridge : MonoBehaviour
    {
        [SerializeField] private Text hpText;
        [SerializeField] private Text xpText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text seedText;

        private EntityManager _entityManager;
        private EntityQuery _playerQuery;
        private EntityQuery _runStateQuery;

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = world.EntityManager;
            _playerQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<PlayerStats>(),
                ComponentType.ReadOnly<Health>());
            _runStateQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RunState>());
        }

        private void OnDestroy()
        {
            if (_playerQuery.IsCreated)
            {
                _playerQuery.Dispose();
            }

            if (_runStateQuery.IsCreated)
            {
                _runStateQuery.Dispose();
            }
        }

        private void Update()
        {
            if (_playerQuery.IsEmpty)
            {
                return;
            }

            var playerEntity = _playerQuery.GetSingletonEntity();
            var stats = _entityManager.GetComponentData<PlayerStats>(playerEntity);
            var health = _entityManager.GetComponentData<Health>(playerEntity);

            if (hpText != null)
            {
                hpText.text = $"HP: {health.Current}/{health.Max}";
            }

            if (xpText != null)
            {
                xpText.text = $"XP: {stats.Xp}/{stats.XpToNext}";
            }

            if (levelText != null)
            {
                levelText.text = $"Level: {stats.Level}";
            }

            if (goldText != null)
            {
                goldText.text = $"Gold: {stats.Gold}";
            }

            if (seedText != null && _runStateQuery.TryGetSingleton<RunState>(out var runState))
            {
                seedText.text = $"Seed: {runState.Seed}";
            }
        }
    }
}
