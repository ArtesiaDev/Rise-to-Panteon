namespace RuntimeRoguelike.Ecs
{
    public class EnemyMoveIntentSystem : IEcsInitSystem, IEcsFixedUpdateSystem
    {
        private EcsPool<GridPosition> _positionPool;
        private EcsPool<TargetEntity> _targetPool;
        private EcsPool<EnemyPath> _pathPool;
        private EcsPool<IdleMoveCooldown> _idlePool;
        private EcsPool<MoveIntent> _intentPool;

        public void Init(EcsWorld world, EcsCommandBuffer commandBuffer)
        {
            _positionPool = world.GetPool<GridPosition>();
            _targetPool = world.GetPool<TargetEntity>();
            _pathPool = world.GetPool<EnemyPath>();
            _idlePool = world.GetPool<IdleMoveCooldown>();
            _intentPool = world.GetPool<MoveIntent>();
        }

        public void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            world.TryGetResource<RunRandom>(out var rng);

            foreach (var entity in world.Query<EnemyTag, GridPosition, TargetEntity, EnemyPath, IdleMoveCooldown>())
            {
                ref var target = ref _targetPool.GetRef(entity);
                ref var path = ref _pathPool.GetRef(entity);
                ref var idle = ref _idlePool.GetRef(entity);

                if (target.HasTarget && path.Steps != null && path.Index < path.Steps.Count)
                {
                    var current = _positionPool.GetRef(entity).Value;
                    var next = path.Steps[path.Index];
                    var moveDirection = new Int2(next.X - current.X, next.Y - current.Y);
                    _intentPool.Add(entity).Direction = moveDirection;
                    continue;
                }

                if (idle.Remaining > 0f)
                {
                    _intentPool.RemoveEntity(entity);
                    continue;
                }

                var roll = rng != null && rng.Core != null ? rng.Core.Next(0, 4) : 0;
                Int2 direction;
                switch (roll)
                {
                    case 0:
                        direction = Int2.Up;
                        break;
                    case 1:
                        direction = Int2.Right;
                        break;
                    case 2:
                        direction = Int2.Down;
                        break;
                    default:
                        direction = Int2.Left;
                        break;
                }

                _intentPool.Add(entity).Direction = direction;
                idle.Remaining = idle.Interval;
            }
        }
    }
}
