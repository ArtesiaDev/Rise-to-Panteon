using System;

namespace RuntimeRoguelike.Ecs
{
    public class EcsPool<T> : IEcsPool where T : struct
    {
        private int[] _denseEntities;
        private T[] _denseComponents;
        private int[] _sparseIndices;
        private int _denseCount;

        public int DenseCount => _denseCount;
        public int[] DenseEntities => _denseEntities;
        public T[] DenseComponents => _denseComponents;

        public EcsPool(int initialCapacity = 128)
        {
            var capacity = Math.Max(16, initialCapacity);
            _denseEntities = new int[capacity];
            _denseComponents = new T[capacity];
            _sparseIndices = new int[capacity];
            for (var i = 0; i < _sparseIndices.Length; i++)
            {
                _sparseIndices[i] = -1;
            }
        }

        public bool Has(int entityId)
        {
            if (entityId < 0 || entityId >= _sparseIndices.Length)
            {
                return false;
            }

            var index = _sparseIndices[entityId];
            return index >= 0 && index < _denseCount && _denseEntities[index] == entityId;
        }

        public ref T Add(int entityId)
        {
            return ref Add(entityId, default);
        }

        public ref T Add(int entityId, T component)
        {
            EnsureSparse(entityId);
            if (Has(entityId))
            {
                var index = _sparseIndices[entityId];
                _denseComponents[index] = component;
                return ref _denseComponents[index];
            }

            EnsureDense(_denseCount + 1);
            _denseEntities[_denseCount] = entityId;
            _denseComponents[_denseCount] = component;
            _sparseIndices[entityId] = _denseCount;
            _denseCount += 1;
            return ref _denseComponents[_denseCount - 1];
        }

        public ref T GetRef(int entityId)
        {
            if (!Has(entityId))
            {
                throw new InvalidOperationException($"Component {typeof(T).Name} not found for entity {entityId}.");
            }

            return ref _denseComponents[_sparseIndices[entityId]];
        }

        public bool TryGet(int entityId, out T component)
        {
            if (!Has(entityId))
            {
                component = default;
                return false;
            }

            component = _denseComponents[_sparseIndices[entityId]];
            return true;
        }

        public void RemoveEntity(int entityId)
        {
            if (!Has(entityId))
            {
                return;
            }

            var index = _sparseIndices[entityId];
            var lastIndex = _denseCount - 1;
            if (index != lastIndex)
            {
                var lastEntity = _denseEntities[lastIndex];
                _denseEntities[index] = lastEntity;
                _denseComponents[index] = _denseComponents[lastIndex];
                _sparseIndices[lastEntity] = index;
            }

            _sparseIndices[entityId] = -1;
            _denseCount -= 1;
        }

        public void Clear()
        {
            for (var i = 0; i < _denseCount; i++)
            {
                var entity = _denseEntities[i];
                if (entity >= 0 && entity < _sparseIndices.Length)
                {
                    _sparseIndices[entity] = -1;
                }
            }

            _denseCount = 0;
        }

        private void EnsureSparse(int entityId)
        {
            if (entityId < _sparseIndices.Length)
            {
                return;
            }

            var newSize = _sparseIndices.Length;
            while (newSize <= entityId)
            {
                newSize *= 2;
            }

            var newSparse = new int[newSize];
            Array.Copy(_sparseIndices, newSparse, _sparseIndices.Length);
            for (var i = _sparseIndices.Length; i < newSparse.Length; i++)
            {
                newSparse[i] = -1;
            }

            _sparseIndices = newSparse;
        }

        private void EnsureDense(int size)
        {
            if (size <= _denseEntities.Length)
            {
                return;
            }

            var newSize = _denseEntities.Length;
            while (newSize < size)
            {
                newSize *= 2;
            }

            Array.Resize(ref _denseEntities, newSize);
            Array.Resize(ref _denseComponents, newSize);
        }
    }
}
