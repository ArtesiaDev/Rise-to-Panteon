using System.Threading;
using Cysharp.Threading.Tasks;
using Framework.Scenes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer.Unity;

namespace Framework
{
    /// <summary>
    /// Точка входа в игру. Инициализирует Addressables и загружает сцены.
    /// </summary>
    public class EntrySceneHandler : IAsyncStartable
    {
        private readonly IScenesLoader _scenesLoader;

        public EntrySceneHandler(IScenesLoader scenesLoader)
        {
            _scenesLoader = scenesLoader;
        }

        public async UniTask StartAsync(CancellationToken ct)
        {
            Debug.Log("[Entry] Initializing Addressables...");
            await Addressables.InitializeAsync().ToUniTask();
            Debug.Log("[Entry] Addressables initialized");

            // Загружаем Main сцену аддитивно (protected, не выгружается)
            Debug.Log("[Entry] Loading Main scene...");
            await _scenesLoader.LoadSceneAsync(new MainScene(), SceneLoadingMode.Additive);
            Debug.Log("[Entry] Main scene loaded");

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // Загружаем Dev сцену аддитивно (protected, только для разработки)
            Debug.Log("[Entry] Loading Dev scene...");
            await _scenesLoader.LoadSceneAsync(new DevScene(), SceneLoadingMode.Additive);
            Debug.Log("[Entry] Dev scene loaded");
#endif

            Debug.Log("[Entry] Boot complete");
        }
    }
}
