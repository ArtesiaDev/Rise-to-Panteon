using System;
using Cysharp.Threading.Tasks;

namespace Framework.Scenes
{
    public enum SceneLoadingMode
    {
        /// <summary>Выгрузить все не-protected сцены перед загрузкой.</summary>
        Single,
        /// <summary>Загрузить аддитивно, не выгружая существующие.</summary>
        Additive
    }

    /// <summary>
    /// Интерфейс загрузчика сцен.
    /// </summary>
    public interface IScenesLoader : IDisposable
    {
        /// <summary>Событие перед началом загрузки сцены (до выгрузки текущей).</summary>
        event Action<IScene> OnSceneLoadingStarted;

        /// <summary>Событие после инициализации новой сцены.</summary>
        event Action<IScene> OnSceneLoadingHandled;

        IScene PreviousScene { get; }
        IScene LoadingScene { get; }
        IScene CurrentActiveScene { get; }

        UniTask LoadSceneAsync(IScene scene, SceneLoadingMode loadingMode = SceneLoadingMode.Single);
    }
}
