using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeRoguelike
{
    [DisallowMultipleComponent]
    public class RunController : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private Vector2Int mapSize = new Vector2Int(200, 200);
        [SerializeField] private KeyCode restartKey = KeyCode.R;
        [SerializeField] private bool randomizeSeedOnStart = true;
        [SerializeField] private int roomAttempts = 60;
        [SerializeField] private int minRoomSize = 6;
        [SerializeField] private int maxRoomSize = 14;
        [SerializeField] private int safeRadius = 6;
        [SerializeField] private float hazardChance = 0.03f;
        [SerializeField] private int initialEnemyCount = 12;
        [SerializeField] private int maxEnemies = 40;
        [SerializeField] private float enemySpawnInterval = 6f;
        [SerializeField] private float difficultyTimeToMax = 300f;
        [SerializeField] private float difficultyMaxStatMultiplier = 2f;
        [SerializeField] private float difficultyMaxSpawnMultiplier = 2f;
        [SerializeField] private float difficultyLevelBonus = 0.05f;
        [SerializeField] private bool enableDevTools = true;

        public int Seed => seed;
        public Vector2Int MapSize => mapSize;
        public Transform WorldRoot { get; private set; }
        public Transform EntitiesRoot { get; private set; }
        public Transform UIRoot { get; private set; }

        private MapGrid mapGrid;
        private MapGenerationResult generationResult;
        private GridOccupancy occupancy;
        private AStarPathfinder pathfinder;
        private RunStats runStats;
        private PlayerController playerController;
        private PlayerStats playerStats;
        private Health playerHealth;
        private DifficultyDirector difficultyDirector;
        private EnemySpawner enemySpawner;
        private HazardSystem hazardSystem;
        private HudController hudController;
        private PerkSelectionUI perkUI;
        private LevelSystem levelSystem;
        private DevTools devTools;

        private void Awake()
        {
            if (gameObject.name != "GodObject")
            {
                gameObject.name = "GodObject";
            }
        }

        private void Start()
        {
            if (randomizeSeedOnStart)
            {
                seed = GenerateSeed();
            }

            GenerateRun();
        }

        private void Update()
        {
            if (restartKey != KeyCode.None && Input.GetKeyDown(restartKey))
            {
                RestartRun();
            }
        }

        public void RestartRun()
        {
            seed = GenerateSeed();
            ClearRun();
            GenerateRun();
        }

        private void GenerateRun()
        {
            CreateRoots();

            runStats = new RunStats();
            runStats.Reset(seed);

            var generator = new MapGenerator(mapSize.x, mapSize.y, seed, roomAttempts, minRoomSize, maxRoomSize, safeRadius);
            generationResult = generator.Generate();
            mapGrid = generationResult.Grid;

            var hazardGenerator = new HazardGenerator(seed, hazardChance);
            hazardGenerator.Populate(mapGrid, generationResult.StartCell, generationResult.SafeRadius);

            occupancy = new GridOccupancy(mapGrid.Width, mapGrid.Height);
            pathfinder = new AStarPathfinder(mapGrid, occupancy);

            var renderer = new TilemapWorldRenderer();
            renderer.Render(mapGrid, WorldRoot);

            CreatePlayer(generationResult.StartCell);
            SetupCamera();
            EnsureEventSystem();
            CreateUI();
            CreateDifficultyDirector();
            CreateEnemySpawner();
            CreateHazardSystem();
            CreateDevTools();

            Debug.Log($"[RunController] Generate run seed {seed}, map {mapSize.x}x{mapSize.y}");
        }

        private void CreatePlayer(Vector2Int startCell)
        {
            var playerObject = new GameObject("Player");
            playerObject.transform.SetParent(EntitiesRoot, false);
            playerObject.transform.position = GridUtils.CellToWorld(startCell);

            var renderer = playerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Get(SpriteKey.Player);

            var body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * 0.8f;

            playerStats = playerObject.AddComponent<PlayerStats>();
            playerHealth = playerObject.AddComponent<Health>();
            playerHealth.Initialize(12, Faction.Player);

            playerObject.AddComponent<StatusEffects>();

            playerController = playerObject.AddComponent<PlayerController>();
            playerController.OnCellChanged += cell => runStats?.SetPlayerCell(cell);
            playerController.Initialize(mapGrid, occupancy, startCell, playerStats);

            var attack = playerObject.AddComponent<PlayerAttack>();
            attack.Initialize(playerStats);
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

            follow.Initialize(playerController.transform);
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

        private void CreateUI()
        {
            var perkObject = new GameObject("PerkUI");
            perkObject.transform.SetParent(UIRoot, false);
            perkUI = perkObject.AddComponent<PerkSelectionUI>();
            perkUI.Initialize();

            levelSystem = playerController.gameObject.AddComponent<LevelSystem>();
            levelSystem.Initialize(playerStats, playerHealth, perkUI, seed);

            var hudObject = new GameObject("HUD");
            hudObject.transform.SetParent(UIRoot, false);
            hudController = hudObject.AddComponent<HudController>();
            hudController.Initialize(this, playerStats, playerHealth, runStats);
        }

        private void CreateDifficultyDirector()
        {
            var difficultyObject = new GameObject("DifficultyDirector");
            difficultyObject.transform.SetParent(transform, false);
            difficultyDirector = difficultyObject.AddComponent<DifficultyDirector>();
            difficultyDirector.Configure(difficultyTimeToMax, difficultyMaxStatMultiplier, difficultyMaxSpawnMultiplier, difficultyLevelBonus);
            difficultyDirector.Initialize();
        }

        private void CreateEnemySpawner()
        {
            var spawnerObject = new GameObject("EnemySpawner");
            spawnerObject.transform.SetParent(EntitiesRoot, false);
            enemySpawner = spawnerObject.AddComponent<EnemySpawner>();
            enemySpawner.Configure(initialEnemyCount, maxEnemies, enemySpawnInterval, 200);
            enemySpawner.Initialize(mapGrid, occupancy, pathfinder, playerController, EntitiesRoot, generationResult.StartCell, generationResult.SafeRadius, difficultyDirector, runStats, seed);
        }

        private void CreateHazardSystem()
        {
            var hazardObject = new GameObject("HazardSystem");
            hazardObject.transform.SetParent(WorldRoot, false);
            hazardSystem = hazardObject.AddComponent<HazardSystem>();
            hazardSystem.Initialize(mapGrid, playerController);
        }

        private void CreateDevTools()
        {
            if (!enableDevTools)
            {
                return;
            }

            var devToolsObject = new GameObject("DevTools");
            devToolsObject.transform.SetParent(transform, false);
            devTools = devToolsObject.AddComponent<DevTools>();
            devTools.Initialize(this, playerController, mapGrid, occupancy, runStats);
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
            root.transform.SetParent(transform, false);
            return root.transform;
        }

        private void ClearRun()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                Destroy(child.gameObject);
            }

            WorldRoot = null;
            EntitiesRoot = null;
            UIRoot = null;
            mapGrid = null;
            generationResult = default;
            occupancy = null;
            pathfinder = null;
            runStats = null;
            playerController = null;
            playerStats = null;
            playerHealth = null;
            difficultyDirector = null;
            enemySpawner = null;
            hazardSystem = null;
            hudController = null;
            perkUI = null;
            levelSystem = null;
            devTools = null;
        }

        private static int GenerateSeed()
        {
            return System.Guid.NewGuid().GetHashCode();
        }
    }
}
