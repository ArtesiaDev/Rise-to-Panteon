using Dev;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Dev.Cheats
{
    /// <summary>
    /// Читы боя: убить врагов, настроить спавн и урон.
    /// </summary>
    public class CheatCombat : ICheat
    {
        private bool _spawnDisabled;
        private int _savedMaxCount;
        private float _savedSpawnInterval;

        public void Initialize(ICheatViewBuilder view)
        {
            view.CreateGroup("Combat")
                .AddCheatButton("Kill All Enemies", OnKillAllEnemies)
                .AddCheatToggle("Disable Enemy Spawning", false, OnToggleSpawning)
                .AddCheatFieldAsInt("Set Player Damage", OnSetDamage)
                .AddCheatFieldAsInt("Set Attack Speed (ms)", OnSetAttackSpeed);
        }

        private void OnKillAllEnemies()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<EnemyTag>());
            var enemies = query.ToEntityArray(Allocator.Temp);
            var count = enemies.Length;

            foreach (var enemy in enemies)
            {
                // Ставим HP = 0, DeathSystem уберёт их корректно
                if (em.HasComponent<Health>(enemy))
                {
                    var hp = em.GetComponentData<Health>(enemy);
                    hp.Current = 0;
                    em.SetComponentData(enemy, hp);
                }
            }

            enemies.Dispose();
            Debug.Log($"[Cheat] Killed {count} enemies");
        }

        private void OnToggleSpawning(bool disabled)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<EnemySpawnConfigData>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var config = em.GetComponentData<EnemySpawnConfigData>(entity);

            if (disabled)
            {
                _savedMaxCount = config.MaxCount;
                _savedSpawnInterval = config.SpawnInterval;
                config.MaxCount = 0;
                config.SpawnInterval = 99999f;
            }
            else
            {
                config.MaxCount = _savedMaxCount > 0 ? _savedMaxCount : config.MaxCount;
                config.SpawnInterval = _savedSpawnInterval > 0 ? _savedSpawnInterval : config.SpawnInterval;
            }

            em.SetComponentData(entity, config);
            _spawnDisabled = disabled;
            Debug.Log($"[Cheat] Enemy Spawning: {(disabled ? "OFF" : "ON")}");
        }

        private void OnSetDamage(int damage)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<Damage>(), ComponentType.ReadOnly<PlayerTag>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            em.SetComponentData(entity, new Damage { Value = damage });
            Debug.Log($"[Cheat] Player Damage = {damage}");
        }

        private void OnSetAttackSpeed(int intervalMs)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<AttackCooldown>(), ComponentType.ReadOnly<PlayerTag>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var cd = em.GetComponentData<AttackCooldown>(entity);
            cd.Interval = intervalMs / 1000f;
            em.SetComponentData(entity, cd);
            Debug.Log($"[Cheat] Attack Interval = {intervalMs}ms");
        }
    }
}
