using UnityEngine;

namespace RuntimeRoguelike.Ecs
{
    public class EcsWorldBehaviour : MonoBehaviour
    {
        private EcsSystemsPipeline _pipeline;
        private GridPositionConverter _gridPositionConverter;
        private bool _awakeTriggered;
        private bool _initExecuted;
        private bool _startTriggered;
        private bool _startExecuted;

        public void Initialize(EcsSystemsPipeline pipeline, GridPositionConverter gridPositionConverter)
        {
            _pipeline = pipeline;
            _gridPositionConverter = gridPositionConverter;
            TryInvokeAwake();
            TryInvokeStart();
        }

        private void Awake()
        {
            _awakeTriggered = true;
            TryInvokeAwake();
        }

        private void Start()
        {
            _startTriggered = true;
            TryInvokeStart();
        }

        private void Update()
        {
            if (!_initExecuted)
            {
                return;
            }

            _pipeline.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!_initExecuted)
            {
                return;
            }

            _pipeline.FixedUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (!_initExecuted)
            {
                return;
            }

            _pipeline.LateUpdate(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_pipeline != null)
            {
                _pipeline.Dispose();
            }
        }

        private void OnDrawGizmos()
        {
            if (!_initExecuted || _pipeline == null || _gridPositionConverter == null)
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
            if (_pipeline == null || !_awakeTriggered || _initExecuted)
            {
                return;
            }

            _pipeline.Init();
            _initExecuted = true;
        }

        private void TryInvokeStart()
        {
            if (_pipeline == null || !_startTriggered || _startExecuted || !_initExecuted)
            {
                return;
            }

            _pipeline.Start();
            _startExecuted = true;
        }
    }
}
