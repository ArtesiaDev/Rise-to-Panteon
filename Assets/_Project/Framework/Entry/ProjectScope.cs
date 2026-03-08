using Framework.Scenes;
using VContainer;
using VContainer.Unity;

namespace Framework
{
    /// <summary>
    /// Корневой DI-скоуп проекта. Живет на EntryScene.
    /// Регистрирует ScenesLoader и глобальные сервисы.
    /// Все сцены наследуют этот скоуп.
    /// </summary>
    public class ProjectScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ScenesLoader>(Lifetime.Singleton)
                .As<IScenesLoader>();

            builder.RegisterEntryPoint<EntrySceneHandler>();
        }
    }
}
