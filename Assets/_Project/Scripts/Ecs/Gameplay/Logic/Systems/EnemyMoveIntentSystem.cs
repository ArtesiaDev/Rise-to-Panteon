namespace RuntimeRoguelike.Ecs
{
    public class EnemyMoveIntentSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            var positionPool = world.GetPool<GridPosition>();
            var targetPool = world.GetPool<TargetEntity>();
            var pathPool = world.GetPool<EnemyPath>();
            var idlePool = world.GetPool<IdleMoveCooldown>();
            var intentPool = world.GetPool<MoveIntent>();

            world.TryGetResource<RunRandom>(out var rng);

            foreach (var entity in world.Query<EnemyTag, GridPosition, TargetEntity, EnemyPath, IdleMoveCooldown>())
            {
                ref var target = ref targetPool.GetRef(entity);
                ref var path = ref pathPool.GetRef(entity);
                ref var idle = ref idlePool.GetRef(entity);

                if (target.HasTarget && path.Steps != null && path.Index < path.Steps.Count)
                {
                    var current = positionPool.GetRef(entity).Value;
                    var next = path.Steps[path.Index];
                    var direction = new Int2(next.X - current.X, next.Y - current.Y);
                    intentPool.Add(entity).Direction = direction;
                    continue;
                }

                if (idle.Remaining > 0f)
                {
                    intentPool.RemoveEntity(entity);
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

                intentPool.Add(entity).Direction = direction;
                idle.Remaining = idle.Interval;
            }
        }
    }
}
