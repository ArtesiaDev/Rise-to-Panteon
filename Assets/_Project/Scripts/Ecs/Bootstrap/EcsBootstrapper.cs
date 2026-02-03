using RuntimeRoguelike.UI.Mvp;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace RuntimeRoguelike.Ecs
{
    public class EcsBootstrapper : IInitializable, System.IDisposable
    {
        private readonly DiContainer _container;
        private readonly GridPositionConverter _gridPositionConverter;
        private readonly UiFactory _uiFactory;
        private readonly EcsCommandService _commands;
        private readonly HudModel _hudModel;
        private readonly HudPresenter _hudPresenter;
        private readonly PerkSelectionModel _perkModel;
        private readonly PerkSelectionPresenter _perkPresenter;

        private EcsWorld _world;
        private EcsSystemsPipeline _pipeline;
        private EcsWorldBehaviour _worldBehaviour;
        private WorldRoots _worldRoots;
        private HudView _hudView;
        private PerkSelectionView _perkView;

        public EcsBootstrapper(
            DiContainer container,
            GridPositionConverter gridPositionConverter,
            UiFactory uiFactory,
            EcsCommandService commands,
            HudModel hudModel,
            HudPresenter hudPresenter,
            PerkSelectionModel perkModel,
            PerkSelectionPresenter perkPresenter)
        {
            _container = container;
            _gridPositionConverter = gridPositionConverter;
            _uiFactory = uiFactory;
            _commands = commands;
            _hudModel = hudModel;
            _hudPresenter = hudPresenter;
            _perkModel = perkModel;
            _perkPresenter = perkPresenter;
        }

        public void Initialize()
        {
            _world = new EcsWorld();
            _commands.SetWorld(_world);

            EnsureRoots();
            _container.BindInstance(_worldRoots).AsSingle();

            _pipeline = new EcsSystemsPipeline(_world);
            RegisterSystems();
            _pipeline.SortSystems();
            _pipeline.PreInit();

            var godObject = _worldRoots.WorldRoot.parent != null ? _worldRoots.WorldRoot.parent.gameObject : _worldRoots.WorldRoot.gameObject;
            _worldBehaviour = godObject.GetComponent<EcsWorldBehaviour>();
            if (_worldBehaviour == null)
            {
                _worldBehaviour = godObject.AddComponent<EcsWorldBehaviour>();
            }

            _worldBehaviour.Initialize(_pipeline, _gridPositionConverter);
            SetupUi();
        }

        public void Dispose()
        {
            _hudPresenter?.Unbind();
            _perkPresenter?.Unbind();
        }

        private void EnsureRoots()
        {
            var god = GameObject.Find("GodObject");
            if (god == null)
            {
                god = new GameObject("GodObject");
            }

            _worldRoots = new WorldRoots
            {
                WorldRoot = CreateChildRoot(god.transform, "WorldRoot"),
                EntitiesRoot = CreateChildRoot(god.transform, "EntitiesRoot"),
                UiRoot = CreateChildRoot(god.transform, "UIRoot"),
                PoolRoot = CreateChildRoot(god.transform, "ViewPoolRoot")
            };

            ClearChildren(_worldRoots.WorldRoot);
            ClearChildren(_worldRoots.EntitiesRoot);
            ClearChildren(_worldRoots.UiRoot);
            ClearChildren(_worldRoots.PoolRoot);
        }

        private Transform CreateChildRoot(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root.transform;
        }

        private void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }
        }

        private void RegisterSystems()
        {
            _pipeline.AddSystem(_container.Instantiate<RunInitSystem>());
            _pipeline.AddSystem(_container.Instantiate<RestartSystem>());
            _pipeline.AddSystem(_container.Instantiate<MapRenderSystem>());
            _pipeline.AddSystem(_container.Instantiate<PlayerInputSystem>());
            _pipeline.AddSystem(_container.Instantiate<DevToolsSystemEcs>());

            _pipeline.AddSystem(_container.Instantiate<CooldownSystem>());
            _pipeline.AddSystem(_container.Instantiate<DifficultySystemEcs>());
            _pipeline.AddSystem(_container.Instantiate<EnemySpawnerSystem>());
            _pipeline.AddSystem(_container.Instantiate<EnemyTargetSystem>());
            _pipeline.AddSystem(_container.Instantiate<EnemyPathSystem>());
            _pipeline.AddSystem(_container.Instantiate<EnemyMoveIntentSystem>());
            _pipeline.AddSystem(_container.Instantiate<MovementSystem>());
            _pipeline.AddSystem(_container.Instantiate<PlayerAttackSystem>());
            _pipeline.AddSystem(_container.Instantiate<EnemyAttackSystem>());
            _pipeline.AddSystem(_container.Instantiate<HazardSystemEcs>());
            _pipeline.AddSystem(_container.Instantiate<PoisonSystem>());
            _pipeline.AddSystem(_container.Instantiate<PickupCollectSystem>());
            _pipeline.AddSystem(_container.Instantiate<DeathSystem>());
            _pipeline.AddSystem(_container.Instantiate<LevelProgressSystem>());

            _pipeline.AddSystem(_container.Instantiate<PerkApplySystem>());
            _pipeline.AddSystem(_container.Instantiate<HudModelSyncSystem>());
            _pipeline.AddSystem(_container.Instantiate<PerkUiBridgeSystem>());
            _pipeline.AddSystem(_container.Instantiate<ViewSpawnSystem>());
            _pipeline.AddSystem(_container.Instantiate<ViewSyncSystem>());
            _pipeline.AddSystem(_container.Instantiate<CameraFollowSystem>());
            _pipeline.AddSystem(_container.Instantiate<DestroyViewSystem>());
        }

        private void SetupUi()
        {
            EnsureEventSystem();

            var hudObject = new GameObject("HudView");
            hudObject.transform.SetParent(_worldRoots.UiRoot, false);
            _hudView = hudObject.AddComponent<HudView>();
            _hudView.Initialize(_uiFactory);
            _hudPresenter.Bind(_hudModel, _hudView, _commands);

            var perkObject = new GameObject("PerkView");
            perkObject.transform.SetParent(_worldRoots.UiRoot, false);
            _perkView = perkObject.AddComponent<PerkSelectionView>();
            _perkView.Initialize(_uiFactory);
            _perkPresenter.Bind(_perkModel, _perkView, _commands);
        }

        private void EnsureEventSystem()
        {
            if (_worldRoots.UiRoot != null && _worldRoots.UiRoot.GetComponentInChildren<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(_worldRoots.UiRoot, false);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
