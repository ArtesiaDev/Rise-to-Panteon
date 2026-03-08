using Dev;
using VContainer;

namespace Dev.Cheats
{
    public static class CheatsInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<CheatGeneral>(Lifetime.Singleton).As<ICheat>();
            builder.Register<CheatVisibility>(Lifetime.Singleton).As<ICheat>();
            builder.Register<CheatCombat>(Lifetime.Singleton).As<ICheat>();
            builder.Register<CheatPlayer>(Lifetime.Singleton).As<ICheat>();
        }
    }
}
