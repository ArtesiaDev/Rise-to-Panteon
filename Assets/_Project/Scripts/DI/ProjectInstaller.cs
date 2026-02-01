using RuntimeRoguelike;
using RuntimeRoguelike.Configs;
using UnityEngine;
using Zenject;

namespace DI
{
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private SpriteFactoryConfig _spriteFactoryConfig;
        [SerializeField] private GridConfig _gridConfig;
        [SerializeField] private CameraFollowConfig _cameraFollowConfig;

        public override void InstallBindings()
        {
            BindConfig(_spriteFactoryConfig);
            BindConfig(_gridConfig);
            BindConfig(_cameraFollowConfig);

            Container.Bind<SpriteFactory>().AsSingle();
            Container.Bind<GridPositionConverter>().AsSingle();
            Container.Bind<UiFactory>().AsSingle();
        }

        private void BindConfig<T>(T config) where T : ScriptableObject
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<T>();
            }

            Container.BindInstance(config).AsSingle();
        }
    }
}

