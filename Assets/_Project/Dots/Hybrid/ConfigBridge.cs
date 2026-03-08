using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    /// <summary>
    /// Создаёт ECS config entity из ScriptableObject-профиля при старте.
    /// Каждый SO в профиле реализует IConfigApplier и сам записывает свои данные в entity.
    /// </summary>
    public class ConfigBridge : MonoBehaviour
    {
        [SerializeField] private ConfigProfileSO _profile;

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[ConfigBridge] DefaultGameObjectInjectionWorld is null");
                return;
            }

            var em = world.EntityManager;

            // Защита от повторного создания конфига
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<RunConfigData>());
            if (!query.IsEmpty)
            {
                Debug.Log("[ConfigBridge] Config entity уже существует, пропускаем");
                return;
            }

            if (_profile == null)
            {
                Debug.LogError("[ConfigBridge] ConfigProfileSO не назначен!");
                return;
            }

            var configEntity = em.CreateEntity();
#if UNITY_EDITOR
            em.SetName(configEntity, "DotsConfig");
#endif

            foreach (var config in _profile.Configs)
            {
                if (config is IConfigApplier applier)
                {
                    applier.Apply(configEntity, em);
                }
                else if (config != null)
                {
                    Debug.LogWarning($"[ConfigBridge] {config.name} не реализует IConfigApplier, пропускаем");
                }
            }

            Debug.Log("[ConfigBridge] Config entity создан успешно");
        }
    }
}
