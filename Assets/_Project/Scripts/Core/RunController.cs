using RuntimeRoguelike.Configs;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace RuntimeRoguelike
{
    public class RunController : IInitializable, ITickable
    {
        private readonly DiContainer _container;
        private readonly RunConfig _runConfig;
        private readonly EnemySpawnConfig _enemySpawnConfig;
        private readonly EnemyConfig _enemyConfig;
        private readonly PlayerConfig _playerConfig;
        private readonly DifficultyConfig _difficultyConfig;
        private readonly LootConfig _lootConfig;
        private readonly LevelConfig _levelConfig;
        private readonly PerkConfig _perkConfig;
        private readonly DevToolsConfig _devToolsConfig;
        private readonly CameraFollowConfig _cameraFollowConfig;
        private readonly MapGenerator _mapGenerator;
        private readonly HazardGenerator _hazardGenerator;
        private readonly TilemapWorldRenderer _tilemapRenderer;
        private readonly GridPositionConverter _gridPositionConverter;
        private readonly SpriteFactory _spriteFactory;
        private readonly DifficultyService _difficultyService;

        private GameObject _godObject;
        private MapGrid _mapGrid;
        private MapGenerationResult _generationResult;
        private GridOccupancy _occupancy;
        private AStarPathfinder _pathfinder;
        private RunStats _runStats;
        private PlayerController _playerController;
        private PlayerStats _playerStats;
        private Health _playerHealth;
        private EnemySpawner _enemySpawner;
        private HazardSystem _hazardSystem;
        private LevelSystem _levelSystem;
        private HudController _hudController;
        private PerkSelectionUI _perkUi;
        private DevTools _devTools;

        public int Seed { get; private set; }
        public Vector2Int MapSize { get; private set; }
        public Transform WorldRoot { get; private set; }
        public Transform EntitiesRoot { get; private set; }
        public Transform UIRoot { get; private set; }

        [Inject]
        public RunController(
            DiContainer container,
            RunConfig runConfig,
            EnemySpawnConfig enemySpawnConfig,
            EnemyConfig enemyConfig,
            PlayerConfig playerConfig,
            DifficultyConfig difficultyConfig,
            LootConfig lootConfig,
            LevelConfig levelConfig,
            PerkConfig perkConfig,
            DevToolsConfig devToolsConfig,
            CameraFollowConfig cameraFollowConfig,
            MapGenerator mapGenerator,
            HazardGenerator hazardGenerator,
            TilemapWorldRenderer tilemapRenderer,
            GridPositionConverter gridPositionConverter,
            SpriteFactory spriteFactory,
            DifficultyService difficultyService)
        {
            _container = container;
            _runConfig = runConfig;
            _enemySpawnConfig = enemySpawnConfig;
            _enemyConfig = enemyConfig;
            _playerConfig = playerConfig;
            _difficultyConfig = difficultyConfig;
            _lootConfig = lootConfig;
            _levelConfig = levelConfig;
            _perkConfig = perkConfig;
            _devToolsConfig = devToolsConfig;
            _cameraFollowConfig = cameraFollowConfig;
            _mapGenerator = mapGenerator;
            _hazardGenerator = hazardGenerator;
            _tilemapRenderer = tilemapRenderer;
            _gridPositionConverter = gridPositionConverter;
            _spriteFactory = spriteFactory;
            _difficultyService = difficultyService;
        }

        public void Initialize()
        {
            EnsureGodObject();
            Seed = _runConfig.RandomizeSeedOnStart ? GenerateSeed() : _runConfig.InitialSeed;
            MapSize = _runConfig.MapSize;
            GenerateRun();
        }

        public void Tick()
        {
            if (_runConfig.RestartKey != KeyCode.None && Input.GetKeyDown(_runConfig.RestartKey))
            {
                RestartRun();
            }

            _enemySpawner?.Tick();
            _hazardSystem?.Tick();
        }

        public void RestartRun()
        {
            Seed = GenerateSeed();
            MapSize = _runConfig.MapSize;
            ClearRun();
            GenerateRun();
        }

        private void GenerateRun()
        {
            CreateRoots();

            _runStats = new RunStats();
            _runStats.Reset(Seed);

            _generationResult = _mapGenerator.Generate(MapSize, Seed);
            _mapGrid = _generationResult.Grid;

            _hazardGenerator.Populate(_mapGrid, _generationResult.StartCell, _generationResult.SafeRadius, Seed);

            _occupancy = new GridOccupancy(_mapGrid.Width, _mapGrid.Height);
            _pathfinder = new AStarPathfinder(_mapGrid, _occupancy);

            _tilemapRenderer.Render(_mapGrid, WorldRoot);

            CreatePlayer(_generationResult.StartCell);
            SetupCamera();
            EnsureEventSystem();
            CreateUi();

            _difficultyService.Configure(_difficultyConfig);
            _difficultyService.Initialize();

            CreateEnemySpawner();
            CreateHazardSystem();
            CreateDevTools();

            Debug.Log($"[RunController] Generate run seed {Seed}, map {MapSize.x}x{MapSize.y}");
        }

        private void CreatePlayer(Vector2Int startCell)
        {
            var playerObject = new GameObject("Player");
            playerObject.transform.SetParent(EntitiesRoot, false);
            playerObject.transform.position = _gridPositionConverter.CellToWorld(startCell);

            var renderer = playerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _spriteFactory.GetSprite(SpriteKey.Player);

            var body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = _playerConfig.ColliderSize;

            _playerStats = playerObject.AddComponent<PlayerStats>();

            _playerHealth = _container.InstantiateComponent<Health>(playerObject);
            _playerHealth.Initialize(_playerConfig.MaxHealth, Faction.Player);

            _container.InstantiateComponent<StatusEffects>(playerObject);

            _playerController = _container.InstantiateComponent<PlayerController>(playerObject);
            _playerController.Initialize(_mapGrid, _occupancy, startCell, _playerStats);
            _playerController.OnCellChanged += cell => _runStats?.SetPlayerCell(cell);

            var attack = _container.InstantiateComponent<PlayerAttack>(playerObject);
            attack.Initialize(_playerStats);
        }

        private void SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var follow = camera.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<CameraFollow>();
            }

            follow.Initialize(_playerController.transform, _cameraFollowConfig);
        }

        private void EnsureEventSystem()
        {
            if (UIRoot != null && UIRoot.GetComponentInChildren<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(UIRoot, false);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void CreateUi()
        {
            var perkObject = new GameObject("PerkUI");
            perkObject.transform.SetParent(UIRoot, false);
            _perkUi = _container.InstantiateComponent<PerkSelectionUI>(perkObject);
            _perkUi.Initialize();

            _levelSystem = _container.Instantiate<LevelSystem>();
            _levelSystem.Initialize(_playerStats, _playerHealth, _perkUi, _perkConfig, _levelConfig, Seed);

            var hudObject = new GameObject("HUD");
            hudObject.transform.SetParent(UIRoot, false);
            _hudController = _container.InstantiateComponent<HudController>(hudObject);
            _hudController.Initialize(this, _playerStats, _playerHealth, _runStats);
        }

        private void CreateEnemySpawner()
        {
            _enemySpawner = _container.Instantiate<EnemySpawner>();
            _enemySpawner.Initialize(
                _mapGrid,
                _occupancy,
                _pathfinder,
                _playerController,
                EntitiesRoot,
                _generationResult.StartCell,
                _generationResult.SafeRadius,
                _enemySpawnConfig,
                _enemyConfig,
                _difficultyService,
                _lootConfig,
                _runStats,
                Seed);
        }

        private void CreateHazardSystem()
        {
            _hazardSystem = _container.Instantiate<HazardSystem>();
            _hazardSystem.Initialize(_mapGrid, _playerController);
        }

        private void CreateDevTools()
        {
            if (!_devToolsConfig.Enabled)
            {
                return;
            }

            var devToolsObject = new GameObject("DevTools");
            devToolsObject.transform.SetParent(_godObject.transform, false);
            _devTools = _container.InstantiateComponent<DevTools>(devToolsObject);
            _devTools.Initialize(this, _playerController, _mapGrid, _occupancy, _runStats);
        }

        private void CreateRoots()
        {
            WorldRoot = CreateChildRoot("WorldRoot");
            EntitiesRoot = CreateChildRoot("EntitiesRoot");
            UIRoot = CreateChildRoot("UIRoot");
        }

        private Transform CreateChildRoot(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(_godObject.transform, false);
            return root.transform;
        }

        private void ClearRun()
        {
            if (_godObject == null)
            {
                return;
            }

            for (var i = _godObject.transform.childCount - 1; i >= 0; i--)
            {
                var child = _godObject.transform.GetChild(i);
                Object.Destroy(child.gameObject);
            }

            WorldRoot = null;
            EntitiesRoot = null;
            UIRoot = null;
            _mapGrid = null;
            _generationResult = default;
            _occupancy = null;
            _pathfinder = null;
            _runStats = null;
            _playerController = null;
            _playerStats = null;
            _playerHealth = null;
            _enemySpawner = null;
            _hazardSystem = null;
            _hudController = null;
            _perkUi = null;
            _levelSystem = null;
            _devTools = null;
        }

        private void EnsureGodObject()
        {
            var existing = GameObject.Find("GodObject");
            _godObject = existing != null ? existing : new GameObject("GodObject");
        }

        private int GenerateSeed()
        {
            return System.Guid.NewGuid().GetHashCode();
        }
    }
}
