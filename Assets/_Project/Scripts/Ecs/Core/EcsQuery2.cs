namespace RuntimeRoguelike.Ecs
{
    public struct EcsQuery<T1, T2>
        where T1 : struct
        where T2 : struct
    {
        private readonly EcsPool<T1> _pool1;
        private readonly EcsPool<T2> _pool2;
        private readonly bool _primaryIsFirst;
        private readonly int[] _primaryEntities;
        private readonly int _primaryCount;

        public EcsQuery(EcsWorld world)
        {
            _pool1 = world.GetPool<T1>();
            _pool2 = world.GetPool<T2>();
            if (_pool1.DenseCount <= _pool2.DenseCount)
            {
                _primaryIsFirst = true;
                _primaryEntities = _pool1.DenseEntities;
                _primaryCount = _pool1.DenseCount;
            }
            else
            {
                _primaryIsFirst = false;
                _primaryEntities = _pool2.DenseEntities;
                _primaryCount = _pool2.DenseCount;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_pool1, _pool2, _primaryEntities, _primaryCount, _primaryIsFirst);
        }

        public struct Enumerator
        {
            private readonly EcsPool<T1> _pool1;
            private readonly EcsPool<T2> _pool2;
            private readonly bool _primaryIsFirst;
            private readonly int[] _primaryEntities;
            private readonly int _primaryCount;
            private int _index;
            private int _current;

            public int Current => _current;

            public Enumerator(EcsPool<T1> pool1, EcsPool<T2> pool2, int[] primaryEntities, int primaryCount, bool primaryIsFirst)
            {
                _pool1 = pool1;
                _pool2 = pool2;
                _primaryEntities = primaryEntities;
                _primaryCount = primaryCount;
                _primaryIsFirst = primaryIsFirst;
                _index = -1;
                _current = -1;
            }

            public bool MoveNext()
            {
                while (++_index < _primaryCount)
                {
                    var entity = _primaryEntities[_index];
                    if (_primaryIsFirst)
                    {
                        if (_pool2.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else
                    {
                        if (_pool1.Has(entity))
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
