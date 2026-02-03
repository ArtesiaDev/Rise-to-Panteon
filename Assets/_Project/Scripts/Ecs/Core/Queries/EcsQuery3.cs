namespace RuntimeRoguelike.Ecs
{
    public struct EcsQuery<T1, T2, T3>
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        private readonly EcsPool<T1> _pool1;
        private readonly EcsPool<T2> _pool2;
        private readonly EcsPool<T3> _pool3;
        private readonly int[] _primaryEntities;
        private readonly int _primaryCount;
        private readonly int _primaryIndex;

        public EcsQuery(EcsWorld world)
        {
            _pool1 = world.GetPool<T1>();
            _pool2 = world.GetPool<T2>();
            _pool3 = world.GetPool<T3>();

            _primaryIndex = 1;
            _primaryEntities = _pool1.DenseEntities;
            _primaryCount = _pool1.DenseCount;

            if (_pool2.DenseCount < _primaryCount)
            {
                _primaryIndex = 2;
                _primaryEntities = _pool2.DenseEntities;
                _primaryCount = _pool2.DenseCount;
            }

            if (_pool3.DenseCount < _primaryCount)
            {
                _primaryIndex = 3;
                _primaryEntities = _pool3.DenseEntities;
                _primaryCount = _pool3.DenseCount;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_pool1, _pool2, _pool3, _primaryEntities, _primaryCount, _primaryIndex);
        }

        public struct Enumerator
        {
            private readonly EcsPool<T1> _pool1;
            private readonly EcsPool<T2> _pool2;
            private readonly EcsPool<T3> _pool3;
            private readonly int[] _primaryEntities;
            private readonly int _primaryCount;
            private readonly int _primaryIndex;
            private int _index;
            private int _current;

            public int Current => _current;

            public Enumerator(
                EcsPool<T1> pool1,
                EcsPool<T2> pool2,
                EcsPool<T3> pool3,
                int[] primaryEntities,
                int primaryCount,
                int primaryIndex)
            {
                _pool1 = pool1;
                _pool2 = pool2;
                _pool3 = pool3;
                _primaryEntities = primaryEntities;
                _primaryCount = primaryCount;
                _primaryIndex = primaryIndex;
                _index = -1;
                _current = -1;
            }

            public bool MoveNext()
            {
                while (++_index < _primaryCount)
                {
                    var entity = _primaryEntities[_index];
                    if (_primaryIndex == 1)
                    {
                        if (_pool2.Has(entity) && _pool3.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else if (_primaryIndex == 2)
                    {
                        if (_pool1.Has(entity) && _pool3.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else
                    {
                        if (_pool1.Has(entity) && _pool2.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
