using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Framework.Scenes
{
    /// <summary>
    /// Реализация загрузчика сцен.
    /// Управляет DI-скоупами, загрузкой/выгрузкой сцен через SceneManager и Addressables.
    /// </summary>
    public class ScenesLoader : IScenesLoader
    {
        private struct SceneContainer
        {
            public IScene Scene;
            public SceneScope Scope;
        }

        public event Action<IScene> OnSceneLoadingStarted;
        public event Action<IScene> OnSceneLoadingHandled;

        public IScene PreviousScene { get; private set; }
        public IScene LoadingScene { get; private set; }
        public IScene CurrentActiveScene { get; private set; }

        private readonly List<SceneContainer> _loadedScenes = new(4);
        private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _sceneInstances = new();
        private readonly CancellationTokenSource _cts = new();

        public async UniTask LoadSceneAsync(IScene scene, SceneLoadingMode loadingMode = SceneLoadingMode.Single)
        {
            LoadingScene = scene;
            OnSceneLoadingStarted?.Invoke(scene);

            // Выгрузка сцен (если Single mode)
            if (loadingMode == SceneLoadingMode.Single)
                await UnloadNonProtectedScenes();

            // Определяем parent scope для DI
            SceneScope parentScope = null;
            for (var i = _loadedScenes.Count - 1; i >= 0; i--)
            {
                var container = _loadedScenes[i];
                if (container.Scope != null && !container.Scope.ExcludeFromScopeHierarchy)
                {
                    parentScope = container.Scope;
                    break;
                }
            }

            // Устанавливаем parent scope для следующей загружаемой LifetimeScope
            if (parentScope != null)
                LifetimeScope.EnqueueParent(parentScope);

            // Загрузка Unity-сцены
            Scene loadedScene;
            if (scene.IsAddressableScene)
            {
                var handle = Addressables.LoadSceneAsync(scene.Name, LoadSceneMode.Additive);
                await handle.ToUniTask();
                _sceneInstances[scene.Name] = handle;
                loadedScene = handle.Result.Scene;
            }
            else
            {
                await SceneManager.LoadSceneAsync(scene.Name, LoadSceneMode.Additive).ToUniTask();
                loadedScene = SceneManager.GetSceneByName(scene.Name);
            }

            // Активация сцены
            SceneManager.SetActiveScene(loadedScene);

            // Поиск SceneScope и SceneHandler в root GameObjects
            SceneScope scope = null;
            SceneHandler handler = null;

            var rootObjects = loadedScene.GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                if (scope == null)
                    scope = go.GetComponent<SceneScope>();
                if (handler == null)
                    handler = go.GetComponent<SceneHandler>();
                if (scope != null && handler != null)
                    break;
            }

            // Сохраняем в список загруженных
            _loadedScenes.Add(new SceneContainer { Scene = scene, Scope = scope });

            // Инициализация SceneHandler (внутри Init() есть свой await Start())
            if (handler != null)
                await handler.Init(_cts.Token);

            // Готово
            PreviousScene = CurrentActiveScene;
            CurrentActiveScene = scene;
            LoadingScene = null;
            OnSceneLoadingHandled?.Invoke(scene);
        }

        private async UniTask UnloadNonProtectedScenes()
        {
            for (var i = _loadedScenes.Count - 1; i >= 0; i--)
            {
                var container = _loadedScenes[i];
                if (container.Scene.IsProtected)
                    continue;

                // Уничтожение DI контейнера
                if (container.Scope != null)
                    container.Scope.Dispose();

                // Выгрузка сцены
                if (container.Scene.IsAddressableScene &&
                    _sceneInstances.TryGetValue(container.Scene.Name, out var handle))
                {
                    await Addressables.UnloadSceneAsync(handle).ToUniTask();
                    _sceneInstances.Remove(container.Scene.Name);
                }
                else
                {
                    await SceneManager.UnloadSceneAsync(container.Scene.Name).ToUniTask();
                }

                _loadedScenes.RemoveAt(i);
            }

            // Очистка неиспользуемых ресурсов
            await Resources.UnloadUnusedAssets().ToUniTask();
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            OnSceneLoadingStarted = null;
            OnSceneLoadingHandled = null;
        }
    }
}
