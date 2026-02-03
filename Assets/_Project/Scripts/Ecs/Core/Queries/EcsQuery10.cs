namespace RuntimeRoguelike.Ecs
{
    public struct EcsQuery<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
        where T6 : struct
        where T7 : struct
        where T8 : struct
        where T9 : struct
        where T10 : struct
    {
        private readonly EcsPool<T1> _pool1;
        private readonly EcsPool<T2> _pool2;
        private readonly EcsPool<T3> _pool3;
        private readonly EcsPool<T4> _pool4;
        private readonly EcsPool<T5> _pool5;
        private readonly EcsPool<T6> _pool6;
        private readonly EcsPool<T7> _pool7;
        private readonly EcsPool<T8> _pool8;
        private readonly EcsPool<T9> _pool9;
        private readonly EcsPool<T10> _pool10;
        private readonly int[] _primaryEntities;
        private readonly int _primaryCount;
        private readonly int _primaryIndex;

        public EcsQuery(EcsWorld world)
        {
            _pool1 = world.GetPool<T1>();
            _pool2 = world.GetPool<T2>();
            _pool3 = world.GetPool<T3>();
            _pool4 = world.GetPool<T4>();
            _pool5 = world.GetPool<T5>();
            _pool6 = world.GetPool<T6>();
            _pool7 = world.GetPool<T7>();
            _pool8 = world.GetPool<T8>();
            _pool9 = world.GetPool<T9>();
            _pool10 = world.GetPool<T10>();

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

            if (_pool4.DenseCount < _primaryCount)
            {
                _primaryIndex = 4;
                _primaryEntities = _pool4.DenseEntities;
                _primaryCount = _pool4.DenseCount;
            }

            if (_pool5.DenseCount < _primaryCount)
            {
                _primaryIndex = 5;
                _primaryEntities = _pool5.DenseEntities;
                _primaryCount = _pool5.DenseCount;
            }

            if (_pool6.DenseCount < _primaryCount)
            {
                _primaryIndex = 6;
                _primaryEntities = _pool6.DenseEntities;
                _primaryCount = _pool6.DenseCount;
            }

            if (_pool7.DenseCount < _primaryCount)
            {
                _primaryIndex = 7;
                _primaryEntities = _pool7.DenseEntities;
                _primaryCount = _pool7.DenseCount;
            }

            if (_pool8.DenseCount < _primaryCount)
            {
                _primaryIndex = 8;
                _primaryEntities = _pool8.DenseEntities;
                _primaryCount = _pool8.DenseCount;
            }

            if (_pool9.DenseCount < _primaryCount)
            {
                _primaryIndex = 9;
                _primaryEntities = _pool9.DenseEntities;
                _primaryCount = _pool9.DenseCount;
            }

            if (_pool10.DenseCount < _primaryCount)
            {
                _primaryIndex = 10;
                _primaryEntities = _pool10.DenseEntities;
                _primaryCount = _pool10.DenseCount;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_pool1, _pool2, _pool3, _pool4, _pool5, _pool6, _pool7, _pool8, _pool9, _pool10, _primaryEntities, _primaryCount, _primaryIndex);
        }

        public struct Enumerator
        {
            private readonly EcsPool<T1> _pool1;
            private readonly EcsPool<T2> _pool2;
            private readonly EcsPool<T3> _pool3;
            private readonly EcsPool<T4> _pool4;
            private readonly EcsPool<T5> _pool5;
            private readonly EcsPool<T6> _pool6;
            private readonly EcsPool<T7> _pool7;
            private readonly EcsPool<T8> _pool8;
            private readonly EcsPool<T9> _pool9;
            private readonly EcsPool<T10> _pool10;
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
                EcsPool<T4> pool4,
                EcsPool<T5> pool5,
                EcsPool<T6> pool6,
                EcsPool<T7> pool7,
                EcsPool<T8> pool8,
                EcsPool<T9> pool9,
                EcsPool<T10> pool10,
                int[] primaryEntities,
                int primaryCount,
                int primaryIndex)
            {
                _pool1 = pool1;
                _pool2 = pool2;
                _pool3 = pool3;
                _pool4 = pool4;
                _pool5 = pool5;
                _pool6 = pool6;
                _pool7 = pool7;
                _pool8 = pool8;
                _pool9 = pool9;
                _pool10 = pool10;
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
                        if (_pool2.Has(entity) && _pool3.Has(entity) && _pool4.Has(entity) && _pool5.Has(entity)
                            && _pool6.Has(entity) && _pool7.Has(entity) && _pool8.Has(entity) && _pool9.Has(entity) && _pool10.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else if (_primaryIndex == 2)
                    {
                        if (_pool1.Has(entity) && _pool3.Has(entity) && _pool4.Has(entity) && _pool5.Has(entity)
                            && _pool6.Has(entity) && _pool7.Has(entity) && _pool8.Has(entity) && _pool9.Has(entity) && _pool10.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else if (_primaryIndex == 3)
                    {
                        if (_pool1.Has(entity) && _pool2.Has(entity) && _pool4.Has(entity) && _pool5.Has(entity)
                            && _pool6.Has(entity) && _pool7.Has(entity) && _pool8.Has(entity) && _pool9.Has(entity) && _pool10.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else if (_primaryIndex == 4)
                    {
                        if (_pool1.Has(entity) && _pool2.Has(entity) && _pool3.Has(entity) && _pool5.Has(entity)
                            && _pool6.Has(entity) && _pool7.Has(entity) && _pool8.Has(entity) && _pool9.Has(entity) && _pool10.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else if (_primaryIndex == 5)
                    {
                        if (_pool1.Has(entity) && _pool2.Has(entity) && _pool3.Has(entity) && _pool4.Has(entity)
                            && _pool6.Has(entity) && _pool7.Has(entity) && _pool8.Has(entity) && _pool9.Has(entity) && _pool10.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else if (_primaryIndex == 6)
                    {
                        if (_pool1.Has(entity) && _pool2.Has(entity) && _pool3.Has(entity) && _pool4.Has(entity)
                            && _pool5.Has(entity) && _pool7.Has(entity) && _pool8.Has(entity) && _pool9.Has(entity) && _pool10.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else if (_primaryIndex == 7)
                    {
                        if (_pool1.Has(entity) && _pool2.Has(entity) && _pool3.Has(entity) && _pool4.Has(entity)
                            && _pool5.Has(entity) && _pool6.Has(entity) && _pool8.Has(entity) && _pool9.Has(entity) && _pool10.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else if (_primaryIndex == 8)
                    {
                        if (_pool1.Has(entity) && _pool2.Has(entity) && _pool3.Has(entity) && _pool4.Has(entity)
                            && _pool5.Has(entity) && _pool6.Has(entity) && _pool7.Has(entity) && _pool9.Has(entity) && _pool10.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else if (_primaryIndex == 9)
                    {
                        if (_pool1.Has(entity) && _pool2.Has(entity) && _pool3.Has(entity) && _pool4.Has(entity)
                            && _pool5.Has(entity) && _pool6.Has(entity) && _pool7.Has(entity) && _pool8.Has(entity) && _pool10.Has(entity))
                        {
                            _current = entity;
                            return true;
                        }
                    }
                    else
                    {
                        if (_pool1.Has(entity) && _pool2.Has(entity) && _pool3.Has(entity) && _pool4.Has(entity)
                            && _pool5.Has(entity) && _pool6.Has(entity) && _pool7.Has(entity) && _pool8.Has(entity) && _pool9.Has(entity))
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
