# Scene Loading & Unloading System

## Overview

Система управления сценами построена на **Addressables**, **VContainer DI**, **UniTask** и кастомном `ScenesLoader`. Сцены переключаются через единый `IScenesLoader`, который автоматически управляет DI-скоупами, выгрузкой ресурсов и lifecycle-хуками.

---

## Core Files

| Файл | Роль |
|------|------|
| `Assets/Sources/Framework/Scenes/IScenesLoader.cs` | Интерфейс загрузчика сцен |
| `Assets/Sources/Framework/Scenes/Internal/SceneLoader.cs` | Реализация `ScenesLoader` + интерфейс `IScene` |
| `Assets/Sources/Framework/Scenes/SceneHandler.cs` | Базовый класс обработчика сцены |
| `Assets/Sources/Framework/Scenes/BaseScenes.cs` | Определения EntryScene, MainScene |
| `Assets/Sources/Framework/Common/VContainer/SceneScope.cs` | VContainer LifetimeScope для сцен |
| `Assets/Sources/Framework/Common/VContainer/MonoBuilderInstaller.cs` | Базовый класс инсталлеров DI |

---

## Архитектура

### IScene — описание сцены

```
Assets/Sources/Framework/Scenes/Internal/SceneLoader.cs:15-21
```

```csharp
public interface IScene
{
    string Name { get; }              // Имя Unity-сцены (совпадает с файлом)
    bool IsProtected { get; }         // Не выгружается при переключении
    bool IsScheduleEmulationEnabled { get; } // Эмуляция таймеров (Map=true, City=false)
    bool IsAddressableScene { get; }  // true = Addressables, false = SceneManager
}
```

### IScenesLoader — интерфейс загрузчика

```
Assets/Sources/Framework/Scenes/IScenesLoader.cs
```

```csharp
public interface IScenesLoader : IDisposable
{
    event Action<IScene> OnSceneLoadingStarted;   // Перед выгрузкой текущей
    event Action<IScene> OnSceneLoadingHandled;   // После инициализации новой
    IScene PreviousScene { get; }
    IScene LoadingScene { get; }
    IScene CurrentActiveScene { get; }

    UniTask LoadSceneAsync(IScene scene, SceneLoadingMode loadingMode = Single,
        AbstractAsyncCommand performAndWaitCommand = null,
        AbstractAsyncCommand performAndForgetCommand = null);

    UniTask LoadOrSwitchActiveScene(IScene scene, ...);
    UniTask<(bool successful, Error error)> TryLoadSceneAsync(...);
}
```

### ScenesLoader — реализация

```
Assets/Sources/Framework/Scenes/Internal/SceneLoader.cs
```

Хранит загруженные сцены в `List<SceneContainer>`:

```csharp
private struct SceneContainer
{
    public IScene Scene;
    public SceneScope Scope;  // VContainer scope этой сцены
}

private List<SceneContainer> _loadedScenes = new(3);
private Dictionary<string, AsyncOperationHandle<SceneInstance>> _sceneInstances = new();
```

---

## Полный цикл загрузки сцены

```
LoadSceneAsync(new CityScene()) вызван
    |
    v
1. LoadingScene = scene; fire OnSceneLoadingStarted
    |
    v
2. UnloadScenes() — выгрузка (подробнее ниже)
    |
    v
3. Определение parent scope для DI
   - Берется последний не-excluded SceneScope из _loadedScenes
    |
    v
4. Загрузка Unity-сцены
   - Addressables: Addressables.LoadSceneAsync(name, LoadSceneMode.Additive)
   - Built-in: SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive)
    |
    v
5. Активация сцены
   - Addressables: SceneInstance.ActivateAsync()
   - Built-in: SceneManager.SetActiveScene()
    |
    v
6. Поиск SceneScope и SceneHandler в root GameObjects сцены
   - SceneScope -> сохраняется в _loadedScenes[i].Scope
   - SceneScope.Configure() вызывает все installers -> создается DI контейнер
    |
    v
7. UniTask.Yield() — ждем MonoBehaviour.Start() для всех объектов сцены
    |
    v
8. handler.Init() — вызов SceneHandler
   - Ждет _started == true (Start() вызван)
   - Вызывает HandleInit() (абстрактный, реализован в каждой сцене)
   - Выполняет performAndWaitCommand (await)
   - Выполняет performAndForgetCommand (fire-and-forget)
    |
    v
9. fire OnSceneLoadingHandled; LoadingScene = null
```

### Выгрузка (UnloadScenes)

```
Assets/Sources/Framework/Scenes/Internal/SceneLoader.cs:136-163
```

**Single mode** (по умолчанию): выгружает все не-protected сцены.
**Additive mode**: оставляет все существующие сцены.

Процесс выгрузки:
1. Итерация `_loadedScenes` в обратном порядке
2. Пропуск сцен с `IsProtected == true`
3. `SceneScope.Dispose()` — уничтожение VContainer контейнера и всех зависимостей
4. Выгрузка: `Addressables.UnloadSceneAsync()` или `SceneManager.UnloadSceneAsync()`
5. `Resources.UnloadUnusedAssets()` + `GC.Collect()`

---

## SceneHandler — обработчик сцены

```
Assets/Sources/Framework/Scenes/SceneHandler.cs
```

```csharp
public abstract class SceneHandler : MonoBehaviour
{
    [Inject] private readonly IPerformer _performer;
    private bool _started;

    protected abstract UniTask HandleInit();

    internal async UniTask Init(AbstractAsyncCommand performAndWaitCommand,
        AbstractAsyncCommand performAndForgetCommand, CancellationToken ct)
    {
        while (_started == false)
            await UniTask.Yield(ct);     // Ждем Unity Start()

        await HandleInit();              // Логика конкретной сцены

        if (performAndWaitCommand != null)
            await _performer.PerformAsync(performAndWaitCommand, ct);

        if (performAndForgetCommand != null)
            _performer.PerformAsync(performAndForgetCommand, ct).Forget();
    }

    private void Start() => _started = true;
}
```

---

## SceneScope — DI скоуп сцены

```
Assets/Sources/Framework/Common/VContainer/SceneScope.cs
```

```csharp
public class SceneScope : VContainer.Unity.LifetimeScope
{
    [SerializeField] private bool excludeFromScopeHierarchy;
    [SerializeField] private MonoBuilderInstaller[] installers;
    [SerializeField] private MonoBehaviour[] autoInjectMonoBehaviours;
    [SerializeField] private MonoBehaviour[] preInjectMonoBehaviours;

    protected override void Configure(IContainerBuilder builder)
    {
        // Pre-inject из parent scope
        if (Parent?.Container != null && preInjectMonoBehaviours != null)
            foreach (var mb in preInjectMonoBehaviours)
                Parent.Container.Inject(mb);

        // Запуск всех инсталлеров
        foreach (var installer in installers)
            installer.Install(builder);

        // Auto-inject после создания контейнера
        if (autoInjectMonoBehaviours?.Length > 0)
            builder.RegisterBuildCallback(container => {
                foreach (var mb in autoInjectMonoBehaviours)
                    container.Inject(mb);
            });
    }
}
```

### DI иерархия скоупов

```
[EntryScope] — bootstrap, Addressables
    |
    v
[MainScope] — services, models, operations, commands (protected)
    |
    v
[CityScope] или [MapScope] — взаимоисключающие (Single mode)
    |
    v
[MiniGameScope] и др. — дочерние фичевые скоупы
```

При `Dispose()` скоупа уничтожается весь DI-контейнер и все дочерние скоупы.

---

## Определения сцен

### EntryScene (built-in, protected)

```
Assets/Sources/Framework/Scenes/BaseScenes.cs
```

- Handler: `EntrySceneHandler` (`Assets/Sources/Entry/Context/EntrySceneHandler.cs`)
- НЕ наследует SceneHandler (работает до DI)
- Инициализирует Addressables, скачивает preload-группу, загружает MainScene

### MainScene (addressable, protected)

```
Assets/Sources/Framework/Scenes/BaseScenes.cs
```

- Handler: `MainSceneHandler` (`Assets/Sources/Main/Context/MainSceneHandler.cs`)
- Запускает `LaunchCommand`
- Остается загруженной всю сессию

### CityScene (addressable)

```
Assets/Sources/Project/Scenes/CityScene.cs
```

- Handler: `CitySceneHandler` (`Assets/Sources/City/Context/CitySceneHandler.cs`)
- `IsScheduleEmulationEnabled = false`
- Инициализирует HUD, туториалы
- Стреляет `CityTriggers.SceneLoaded`

### MapScene (addressable)

```
Assets/Sources/Main/Context/MapScene.cs
```

- Handler: `MapSceneHandler` (`Assets/Sources/Map/Context/MapSceneHandler.cs`)
- `IsScheduleEmulationEnabled = true`
- Загружает реалмы, создает карту, инициализирует HUD
- Стреляет `MapTriggers.SceneLoaded`

### MiniGameScene (addressable, dynamic name)

```
Assets/Sources/Project/Scenes/MiniGameScene.cs
```

- Handler: `MiniGameSceneHandler` (`Assets/Sources/MiniGames/Core/MiniGameSceneHandler.cs`)
- `IsScheduleEmulationEnabled = true`
- Имя передается динамически (Arena, Battler, FruitNinja, etc.)

### Cutscene-сцены

```
Assets/Sources/Project/Scenes/TrainCutsceneScene.cs
Assets/Sources/Project/Scenes/CrusherCutsceneScene.cs
```

### Dev-сцены

```
Assets/Sources/Project/Scenes/DevScene.cs       — protected, debug
Assets/Sources/Project/Scenes/MapEditorScene.cs  — map editor
Assets/Sources/Project/Scenes/ModelPreviewScene.cs
```

### Сводная таблица

| Scene | Addressable | Protected | Emulation | Handler |
|-------|------------|-----------|-----------|---------|
| EntryScene | No | Yes | No | EntrySceneHandler |
| MainScene | Yes | Yes | No | MainSceneHandler |
| CityScene | Yes | No | No | CitySceneHandler |
| MapScene | Yes | No | Yes | MapSceneHandler |
| MiniGameScene | Yes | No | Yes | MiniGameSceneHandler |
| TrainCutsceneScene | Yes | No | No | TrainCutsceneHandler |
| CrusherCutsceneScene | Yes | No | No | CrusherCutsceneHandler |
| DevScene | Yes | Yes | No | — |
| MapEditorScene | Yes | No | No | MapEditorHandler |
| TruckParkingScene | Yes | No | No | self-managed |

---

## Команды переключения сцен

### LoadSceneWithTransitionCommand

```
Assets/Sources/Main/Commands/LoadSceneWithTransitionCommand.cs
```

Обертка: показывает `TransitionViewController` (overlay-анимация) -> загружает сцену -> скрывает overlay.

```csharp
var transitionController = _transitionViewControllerFactory.Create();
await transitionController.Show();
await _scenesLoader.LoadSceneAsync(command.Scene);
await transitionController.Hide();
transitionController.Dispose();
```

### ReturnToCityCommand

```
Assets/Sources/Main/Commands/ReturnToCityCommand.cs
```

Map -> City. Использует `TransitionViewController`.

### ReturnToMapCommand

```
Assets/Sources/Main/Commands/ReturnToMapCommand.cs
```

City -> Map. Два режима:
- **LoadWithTransition** — быстрый overlay
- **LoadWithPreloader** — показывает `MapPreloaderService` (подготовка ассетов карты)

---

## UI загрузки

### LoadingViewController

```
Assets/Sources/Project/LoadingView/LoadingView.cs
```

- Загружается из `Resources.Load<GameObject>("LoadingView")`
- Показывает прогресс-бар при запуске игры (LaunchCommand)
- Этапы: Bootstrap -> UpdateStatics -> Authorization -> LoadUserData -> ...
- `SetProgress(float value, string status)` — плавная анимация (0.3s)
- Уничтожается после первой загрузки CityScene/MapScene

### TransitionViewController

```
Assets/Sources/Main/Views/TransitionView/TransitionView.cs
```

- Addressable-ассет (шейдер с `_HideShift` параметром)
- `Show()` — slide-in анимация (DOTween)
- `Hide()` — slide-out анимация
- Используется при переходах City <-> Map, при загрузке мини-игр

### LoadingOverlayUiView

```
Assets/Sources/Common/UiViews/LoadingOverlayUiView.cs
```

Простой overlay с текстом "Loading...", показывается при коротких переходах.

---

## Хуки фичей: ClientTriggers

```
Assets/Sources/Framework/Common/ClientTriggers/IClientTriggers.cs
```

Фичи подписываются на триггеры сцен для инициализации контента:

```csharp
// В CitySceneHandler после HandleInit():
_clientTriggers.FireTrigger(new CityTriggers.SceneLoaded());

// В MapSceneHandler после HandleInit():
_clientTriggers.FireTrigger(new MapTriggers.SceneLoaded());

// В LaunchCommand после первой загрузки:
_clientTriggers.FireTrigger(new MainTriggers.Game.Loaded());
```

Триггеры определены в:
- `Assets/Sources/City/Utils/CityTriggers.cs` — `city.sceneLoaded`
- `Assets/Sources/Map/Utils/MapTriggers.cs` — `map.sceneLoaded`
- `Assets/Sources/Main/Utils/MainTriggers.cs` — `main.game.loaded`, и др.

Фичи слушают триггеры:
```csharp
_triggers.ListenTrigger(new CityTriggers.SceneLoaded(), OnCityLoaded);
```

---

## Полный Entry Flow при запуске

```
1. Unity загружает EntryScene (build index 0)
   -> EntrySceneHandler.Start()
   -> AddressablesInitializeAsync()
   -> LoadDependencies() (preload-группа)
   -> LoadingViewController (прогресс-бар)

2. Загрузка MainScene (Addressable, protected, additive)
   -> Выгрузка EntryScene (scene 0)
   -> MainSceneHandler.HandleInit()
   -> LaunchCommand запускается

3. LaunchCommand
   -> Bootstrap (графика, инициализация)
   -> UpdateStatics (загрузка статиков с сервера)
   -> Parse statics + localization
   -> Authorization
   -> Load remote user data
   -> Emulate lifetime
   -> Initialize features
   -> LoadSceneAsync(CityScene или MapScene)
   -> fire MainTriggers.Game.Loaded()
   -> Dispose LoadingViewController

4. CityScene/MapScene загружена
   -> SceneScope создает DI контейнер (child от MainScope)
   -> SceneHandler.HandleInit()
   -> fire CityTriggers.SceneLoaded / MapTriggers.SceneLoaded
```

---

## Особый случай: TruckParkingScene

```
Assets/Sources/City/TruckParking/TruckParkingScene/TruckParkingSceneService.cs
```

Не использует `ScenesLoader`. Управляется вручную через `TruckParkingSceneService`:
- Загрузка: `Addressables.LoadSceneAsync()` напрямую
- Рендер в `RenderTexture` (отдельная камера)
- Автовыгрузка через 20 секунд неактивности (`ITickable`)
- Не создает SceneScope, не участвует в DI-иерархии
