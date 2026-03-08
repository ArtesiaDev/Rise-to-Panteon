using Dev;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Dev.Cheats
{
    /// <summary>
    /// Общие читы: бессмертие, хил, рестарт рана.
    /// </summary>
    public class CheatGeneral : ICheat
    {
        private bool _godMode;
        private int _savedMaxHp;

        public void Initialize(ICheatViewBuilder view)
        {
            view.CreateGroup("General")
                .AddCheatToggle("God Mode", false, OnGodMode)
                .AddCheatButton("Full Heal", OnFullHeal)
                .AddCheatButton("Restart Run", OnRestartRun)
                .AddCheatFieldAsInt("Set Seed & Restart", OnSetSeed);
        }

        private void OnGodMode(bool enabled)
        {
            _godMode = enabled;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<Health>(), ComponentType.ReadOnly<PlayerTag>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var health = em.GetComponentData<Health>(entity);

            if (enabled)
            {
                _savedMaxHp = health.Max;
                health.Max = 99999;
                health.Current = 99999;
            }
            else
            {
                health.Max = _savedMaxHp > 0 ? _savedMaxHp : health.Max;
                health.Current = health.Max;
            }

            em.SetComponentData(entity, health);
            Debug.Log($"[Cheat] God Mode: {enabled}");
        }

        private void OnFullHeal()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<Health>(), ComponentType.ReadOnly<PlayerTag>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var health = em.GetComponentData<Health>(entity);
            health.Current = health.Max;
            em.SetComponentData(entity, health);
            Debug.Log("[Cheat] Full Heal");
        }

        private void OnRestartRun()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<RunCommand>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var cmd = em.GetComponentData<RunCommand>(entity);
            cmd.Restart = true;
            em.SetComponentData(entity, cmd);
            _godMode = false;
            Debug.Log("[Cheat] Restart Run");
        }

        private void OnSetSeed(int seed)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;

            // Устанавливаем seed
            var runQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RunState>());
            if (!runQuery.IsEmpty)
            {
                var entity = runQuery.GetSingletonEntity();
                var state = em.GetComponentData<RunState>(entity);
                state.Seed = (uint)seed;
                em.SetComponentData(entity, state);
            }

            // Рестарт
            var cmdQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RunCommand>());
            if (!cmdQuery.IsEmpty)
            {
                var entity = cmdQuery.GetSingletonEntity();
                var cmd = em.GetComponentData<RunCommand>(entity);
                cmd.Restart = true;
                em.SetComponentData(entity, cmd);
            }

            _godMode = false;
            Debug.Log($"[Cheat] Set Seed: {seed}, restarting...");
        }
    }
}
