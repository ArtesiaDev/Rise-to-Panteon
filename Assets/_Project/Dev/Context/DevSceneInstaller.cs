using Cheats.Views;
using Dev.Cheats;
using Framework.Scenes;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Dev.Scenes
{
    /// <summary>
    /// DI-скоуп DevScene. Наследует SceneScope для интеграции с ScenesLoader.
    /// </summary>
    public class DevSceneInstaller : SceneScope
    {
        [SerializeField] private CheatsView cheatsView;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterInstance(cheatsView);
            builder.RegisterEntryPoint<DevSceneHandler>();

            CheatsInstaller.Install(builder);
        }
    }
}
