using RuntimeRoguelike;
using RuntimeRoguelike.Configs;
using RuntimeRoguelike.Ecs;
using RuntimeRoguelike.UI.Mvp;
using UnityEngine;
using Zenject;

namespace DI
{
    public class MainInstaller : MonoInstaller
    {
        [SerializeField] private RunConfig _runConfig;
        [SerializeField] private MapGenerationConfig _mapGenerationConfig;
        [SerializeField] private HazardConfig _hazardConfig;
        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField] private EnemySpawnConfig _enemySpawnConfig;
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private PlayerAttackConfig _playerAttackConfig;
        [SerializeField] private DifficultyConfig _difficultyConfig;
        [SerializeField] private LootConfig _lootConfig;
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private PerkConfig _perkConfig;
        [SerializeField] private DevToolsConfig _devToolsConfig;

        public override void InstallBindings()
        {
            BindConfig(_runConfig);
            BindConfig(_mapGenerationConfig);
            BindConfig(_hazardConfig);
            BindConfig(_enemyConfig);
            BindConfig(_enemySpawnConfig);
            BindConfig(_playerConfig);
            BindConfig(_playerAttackConfig);
            BindConfig(_difficultyConfig);
            BindConfig(_lootConfig);
            BindConfig(_levelConfig);
            BindConfig(_perkConfig);
            BindConfig(_devToolsConfig);

            Container.Bind<MapGenerator>().AsSingle();
            Container.Bind<HazardGenerator>().AsSingle();
            Container.Bind<TilemapWorldRenderer>().AsSingle();
            Container.Bind<PathfindingService>().AsSingle();
            Container.Bind<EcsEntityFactory>().AsSingle();
            Container.Bind<RunInitializer>().AsSingle();
            Container.Bind<IInputService>().To<UnityInputService>().AsSingle();
            Container.Bind<EcsCommandService>().AsSingle();
            Container.Bind<EntityViewFactory>().AsSingle();
            Container.Bind<EntityViewPool>().AsSingle();
            Container.Bind<EntityViewRegistry>().AsSingle();
            Container.Bind<HudModel>().AsSingle();
            Container.Bind<HudPresenter>().AsSingle();
            Container.Bind<PerkSelectionModel>().AsSingle();
            Container.Bind<PerkSelectionPresenter>().AsSingle();

            Container.BindInterfacesAndSelfTo<EcsBootstrapper>().AsSingle();
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