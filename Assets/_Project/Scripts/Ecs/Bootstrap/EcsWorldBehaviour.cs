using UnityEngine;

namespace RuntimeRoguelike.Ecs
{
    public class EcsWorldBehaviour : MonoBehaviour
    {
        private EcsSystemsPipeline _pipeline;
        private GridPositionConverter _gridPositionConverter;
        private bool _initialized;

        public void Initialize(EcsSystemsPipeline pipeline, GridPositionConverter gridPositionConverter)
        {
            _pipeline = pipeline;
            _gridPositionConverter = gridPositionConverter;
            _pipeline.SortSystems();
            _pipeline.Init();
            _initialized = true;
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
    }
}
