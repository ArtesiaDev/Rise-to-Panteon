using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class FixedStepSettingsBridge : MonoBehaviour
    {
        [SerializeField] private bool _applyOnEnable = true;

        private void OnEnable()
        {
            if (_applyOnEnable)
            {
                Apply();
            }
        }

        private void OnValidate()
        {
            if (_applyOnEnable && Application.isPlaying)
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
