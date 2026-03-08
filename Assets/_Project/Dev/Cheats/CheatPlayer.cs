using Dev;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Dev.Cheats
{
    /// <summary>
    /// Читы игрока: золото, опыт, скорость, телепорт.
    /// </summary>
    public class CheatPlayer : ICheat
    {
        public void Initialize(ICheatViewBuilder view)
        {
            view.CreateGroup("Player")
                .AddCheatFieldAsInt("Add Gold", OnAddGold)
                .AddCheatFieldAsInt("Add XP", OnAddXp)
                .AddCheatButton("Level Up", OnLevelUp)
                .AddCheatFieldAsInt("Set Move Speed %", OnSetMoveSpeed)
                .AddCheatButton("Teleport to Start", OnTeleportToStart);
        }

        private void OnAddGold(int amount)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<PlayerStats>(), ComponentType.ReadOnly<PlayerTag>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var stats = em.GetComponentData<PlayerStats>(entity);
            stats.Gold += amount;
            em.SetComponentData(entity, stats);
            Debug.Log($"[Cheat] Gold +{amount} (total: {stats.Gold})");
        }

        private void OnAddXp(int amount)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<PlayerStats>(), ComponentType.ReadOnly<PlayerTag>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var stats = em.GetComponentData<PlayerStats>(entity);
            stats.Xp += amount;
            em.SetComponentData(entity, stats);
            Debug.Log($"[Cheat] XP +{amount} (total: {stats.Xp}, next: {stats.XpToNext})");
        }

        private void OnLevelUp()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<PlayerStats>(), ComponentType.ReadOnly<PlayerTag>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var stats = em.GetComponentData<PlayerStats>(entity);
            // Добавляем ровно столько XP, сколько нужно для левелапа
            stats.Xp = stats.XpToNext;
            em.SetComponentData(entity, stats);
            Debug.Log($"[Cheat] Level Up! (XP set to {stats.XpToNext})");
        }

        private void OnSetMoveSpeed(int percent)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<PlayerStats>(), ComponentType.ReadOnly<PlayerTag>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var stats = em.GetComponentData<PlayerStats>(entity);
            stats.MoveSpeedMult = percent / 100f;
            em.SetComponentData(entity, stats);
            Debug.Log($"[Cheat] Move Speed = {percent}%");
        }

        private void OnTeleportToStart()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var runQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RunState>());
            if (runQuery.IsEmpty) return;

            var runState = runQuery.GetSingleton<RunState>();

            var inputQuery = em.CreateEntityQuery(ComponentType.ReadWrite<InputState>());
            if (inputQuery.IsEmpty) return;

            var inputEntity = inputQuery.GetSingletonEntity();
            var input = em.GetComponentData<InputState>(inputEntity);
            input.TeleportRequested = true;
            input.TeleportTarget = runState.StartCell;
            em.SetComponentData(inputEntity, input);
            Debug.Log($"[Cheat] Teleport to start: {runState.StartCell}");
        }
    }
}
