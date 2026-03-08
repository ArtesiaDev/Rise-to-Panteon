using Dev;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Dev.Cheats
{
    /// <summary>
    /// Читы видимости: отключить туман войны, разведать всю карту.
    /// </summary>
    public class CheatVisibility : ICheat
    {
        public void Initialize(ICheatViewBuilder view)
        {
            view.CreateGroup("Visibility")
                .AddCheatToggle("Disable Fog of War", false, OnToggleFow)
                .AddCheatButton("Reveal Entire Map", OnRevealMap);
        }

        private void OnToggleFow(bool disabled)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadWrite<VisibilityConfigData>());
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var config = em.GetComponentData<VisibilityConfigData>(entity);
            config.Enabled = !disabled;
            em.SetComponentData(entity, config);

            if (disabled)
            {
                // Разведать всю карту и показать рендер без тумана
                RevealAllCells(world);
            }
            else
            {
                // Сбрасываем позицию чтобы FogOfWarSystem пересчитал
                ForceRecalcFog(world);
            }

            Debug.Log($"[Cheat] Fog of War: {(disabled ? "OFF" : "ON")}");
        }

        private void OnRevealMap()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            RevealAllCells(world);
            Debug.Log("[Cheat] Map revealed");
        }

        private static void RevealAllCells(World world)
        {
            var fogSystemHandle = world.Unmanaged.GetExistingUnmanagedSystem<FogOfWarSystem>();
            if (fogSystemHandle == SystemHandle.Null) return;

            ref var fogSystem = ref world.Unmanaged.GetUnsafeSystemRef<FogOfWarSystem>(fogSystemHandle);
            if (!fogSystem.IsAllocated) return;

            var fogState = fogSystem.GetFogState();
            for (var i = 0; i < fogState.Length; i++)
            {
                fogState[i] = FogState.Visible;
            }

            // Триггерим обновление рендера тумана
            var em = world.EntityManager;
            var mapQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RunState>());
            if (mapQuery.IsEmpty) return;

            var mapEntity = mapQuery.GetSingletonEntity();
            if (em.HasComponent<FogRenderRequest>(mapEntity))
            {
                em.SetComponentEnabled<FogRenderRequest>(mapEntity, true);
            }
        }

        private static void ForceRecalcFog(World world)
        {
            // Сбрасываем _lastPlayerPos через пересоздание — система сама пересчитает
            // Простейший способ: триггерим FogRenderRequest
            var em = world.EntityManager;
            var mapQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RunState>());
            if (mapQuery.IsEmpty) return;

            var mapEntity = mapQuery.GetSingletonEntity();
            if (em.HasComponent<FogRenderRequest>(mapEntity))
            {
                em.SetComponentEnabled<FogRenderRequest>(mapEntity, true);
            }
        }
    }
}
