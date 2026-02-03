namespace RuntimeRoguelike.Ecs
{
    public struct EcsQuery<T1> where T1 : struct
    {
        private readonly EcsPool<T1> _pool1;

        public EcsQuery(EcsWorld world)
        {
            _pool1 = world.GetPool<T1>();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_pool1);
        }

        public struct Enumerator
        {
            private readonly EcsPool<T1> _pool1;
            private int _index;
            private int _current;

            public int Current => _current;

            public Enumerator(EcsPool<T1> pool1)
            {
                _pool1 = pool1;
                _index = -1;
                _current = -1;
            }

            public bool MoveNext()
            {
                var entities = _pool1.DenseEntities;
                var count = _pool1.DenseCount;
                while (++_index < count)
                {
                    _current = entities[_index];
                    return true;
                }

                return false;
            }
        }
    }
}
