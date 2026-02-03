using UnityEngine;

namespace RuntimeRoguelike.Ecs
{
    public class EcsWorldBehaviour : MonoBehaviour
    {
        private EcsSystemsPipeline _pipeline;
        private GridPositionConverter _gridPositionConverter;
        private bool _initialized;
        private bool _awakeTriggered;
        private bool _awakeExecuted;
        private bool _enableTriggered;
        private bool _enableExecuted;
        private bool _startTriggered;
        private bool _startExecuted;

        public void Initialize(EcsSystemsPipeline pipeline, GridPositionConverter gridPositionConverter)
        {
            _pipeline = pipeline;
            _gridPositionConverter = gridPositionConverter;
            _pipeline.SortSystems();
            _pipeline.PreInit();
            _pipeline.Init();
            _pipeline.LateInit();
            _initialized = true;

            TryInvokeAwake();
            TryInvokeEnable();
            TryInvokeStart();
        }

        private void Awake()
        {
            _awakeTriggered = true;
            TryInvokeAwake();
        }

        private void OnEnable()
        {
            _enableTriggered = true;
            TryInvokeEnable();
        }

        private void Start()
        {
            _startTriggered = true;
            TryInvokeStart();
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            _pipeline.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            _pipeline.FixedUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            _pipeline.LateUpdate(Time.deltaTime);
        }

        private void OnDisable()
        {
            if (_initialized && _pipeline != null)
            {
                _pipeline.OnDisable();
            }

            _enableExecuted = false;
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                _pipeline.Dispose();
            }
        }

        private void OnDrawGizmos()
        {
            if (!_initialized || _pipeline == null || _gridPositionConverter == null)
            {
                return;
            }

            var world = _pipeline.World;
            if (!world.TryGetResource<DebugState>(out var debug) || !debug.ShowGizmos)
            {
                return;
            }

            if (!world.TryGetResource<RunCounters>(out var counters))
            {
                return;
            }

            var positionPool = world.GetPool<GridPosition>();
            if (!positionPool.Has(counters.PlayerEntityId))
            {
                return;
            }

            var cell = positionPool.GetRef(counters.PlayerEntityId).Value;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(
                _gridPositionConverter.CellToWorld(new Float2(cell.X, cell.Y)),
                Vector3.one * _gridPositionConverter.CellSize);
        }

        private void TryInvokeAwake()
        {
            if (_pipeline == null || !_awakeTriggered || _awakeExecuted)
            {
                return;
            }

            _pipeline.Awake();
            _awakeExecuted = true;
        }

        private void TryInvokeEnable()
        {
            if (_pipeline == null || !_enableTriggered || _enableExecuted)
            {
                return;
            }

            _pipeline.OnEnable();
            _enableExecuted = true;
        }

        private void TryInvokeStart()
        {
            if (_pipeline == null || !_startTriggered || _startExecuted)
            {
                return;
            }

            _pipeline.Start();
            _startExecuted = true;
        }
    }
}
