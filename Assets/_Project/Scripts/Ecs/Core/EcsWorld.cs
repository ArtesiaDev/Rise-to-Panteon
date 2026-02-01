using System;
using System.Collections.Generic;

namespace RuntimeRoguelike.Ecs
{
    public class EcsWorld
    {
        private readonly Dictionary<Type, IEcsPool> _pools = new Dictionary<Type, IEcsPool>();
        private readonly Dictionary<Type, object> _resources = new Dictionary<Type, object>();
        private readonly Stack<int> _freeIds = new Stack<int>();

        private int[] _generations = new int[128];
        private bool[] _alive = new bool[128];
        private int _nextEntityId;

        public int AliveCount { get; private set; }

        public EcsEntity CreateEntity()
        {
            var id = _freeIds.Count > 0 ? _freeIds.Pop() : _nextEntityId++;
            EnsureCapacity(id);
            _alive[id] = true;
            AliveCount += 1;
            return new EcsEntity(id, _generations[id]);
        }

        public bool IsAlive(EcsEntity entity)
        {
            if (entity.Id < 0 || entity.Id >= _alive.Length)
            {
                return false;
            }

            return _alive[entity.Id] && _generations[entity.Id] == entity.Generation;
        }

        public EcsEntity GetEntity(int entityId)
        {
            if (entityId < 0 || entityId >= _generations.Length)
            {
                return new EcsEntity(-1, -1);
            }

            return new EcsEntity(entityId, _generations[entityId]);
        }

        public int GetGeneration(int entityId)
        {
            if (entityId < 0 || entityId >= _generations.Length)
            {
                return -1;
            }

            return _generations[entityId];
        }

        public void DestroyEntity(EcsEntity entity)
        {
            if (!IsAlive(entity))
            {
                return;
            }

            foreach (var pool in _pools.Values)
            {
                pool.RemoveEntity(entity.Id);
            }

            _alive[entity.Id] = false;
            _generations[entity.Id] += 1;
            _freeIds.Push(entity.Id);
            AliveCount = Math.Max(0, AliveCount - 1);
        }

        public void Clear()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }

            _resources.Clear();
            _freeIds.Clear();
            Array.Clear(_generations, 0, _generations.Length);
            Array.Clear(_alive, 0, _alive.Length);
            _nextEntityId = 0;
            AliveCount = 0;
        }

        public EcsPool<T> GetPool<T>() where T : struct
        {
            var type = typeof(T);
            if (_pools.TryGetValue(type, out var pool))
            {
                return (EcsPool<T>)pool;
            }

            var created = new EcsPool<T>();
            _pools[type] = created;
            return created;
        }

        public void SetResource<T>(T resource)
        {
            _resources[typeof(T)] = resource;
        }

        public bool TryGetResource<T>(out T resource)
        {
            if (_resources.TryGetValue(typeof(T), out var value) && value is T typed)
            {
                resource = typed;
                return true;
            }

            resource = default;
            return false;
        }

        public T GetResource<T>()
        {
            if (TryGetResource<T>(out var resource))
            {
                return resource;
            }

            throw new InvalidOperationException($"ECS resource {typeof(T).Name} not found.");
        }

        public EcsQuery<T1> Query<T1>() where T1 : struct
        {
            return new EcsQuery<T1>(this);
        }

        public EcsQuery<T1, T2> Query<T1, T2>()
            where T1 : struct
            where T2 : struct
        {
            return new EcsQuery<T1, T2>(this);
        }

        public EcsQuery<T1, T2, T3> Query<T1, T2, T3>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            return new EcsQuery<T1, T2, T3>(this);
        }

        public EcsQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            return new EcsQuery<T1, T2, T3, T4>(this);
        }

        private void EnsureCapacity(int entityId)
        {
            if (entityId < _alive.Length)
            {
                return;
            }

            var newSize = _alive.Length;
            while (newSize <= entityId)
            {
                newSize *= 2;
            }

            Array.Resize(ref _alive, newSize);
            Array.Resize(ref _generations, newSize);
        }
    }
}
