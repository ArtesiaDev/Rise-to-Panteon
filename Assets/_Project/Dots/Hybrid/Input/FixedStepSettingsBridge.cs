using RuntimeRoguelike.Dots;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class FixedStepSettingsBridge : MonoBehaviour
    {
        [SerializeField] private bool applyOnEnable = true;

        private void OnEnable()
        {
            if (applyOnEnable)
            {
                Apply();
            }
        }

        private void OnValidate()
        {
            if (applyOnEnable && Application.isPlaying)
            {
                Apply();
            }
        }

        public void Apply()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                return;
            }

            var entityManager = world.EntityManager;
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<FixedStepSettings>());

            if (query.IsEmpty)
            {
                var entity = entityManager.CreateEntity();
                entityManager.AddComponentData(entity, new FixedStepSettings
                {
                    Timestep = Time.fixedDeltaTime,
                    IsSet = true
                });
                return;
            }

            var singleton = query.GetSingletonEntity();
            var settings = entityManager.GetComponentData<FixedStepSettings>(singleton);
            settings.Timestep = Time.fixedDeltaTime;
            settings.IsSet = true;
            entityManager.SetComponentData(singleton, settings);
        }
    }
}
