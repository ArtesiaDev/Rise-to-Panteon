namespace RuntimeRoguelike
{
    public class MinHeap
    {
        private readonly int[] _heap;
        private readonly int[] _positions;
        private readonly int[] _fScore;
        private int _count;

        public int Count => _count;

        public MinHeap(int capacity, int[] fScore)
        {
            _heap = new int[capacity];
            _positions = new int[capacity];
            _fScore = fScore;
            _count = 0;

            for (var i = 0; i < _positions.Length; i++)
            {
                _positions[i] = -1;
            }
        }

        public bool Contains(int index)
        {
            return _positions[index] >= 0;
        }

        public void Push(int index)
        {
            _heap[_count] = index;
            _positions[index] = _count;
            BubbleUp(_count);
            _count++;
        }

        public int Pop()
        {
            var min = _heap[0];
            _count--;
            if (_count > 0)
            {
                _heap[0] = _heap[_count];
                _positions[_heap[0]] = 0;
                BubbleDown(0);
            }

            _positions[min] = -1;
            return min;
        }

        public void Update(int index)
        {
            var position = _positions[index];
            if (position < 0)
            {
                return;
            }

            if (!BubbleUp(position))
            {
                BubbleDown(position);
            }
        }

        private bool BubbleUp(int index)
        {
            var moved = false;
            while (index > 0)
            {
                var parent = (index - 1) / 2;
                if (Compare(_heap[index], _heap[parent]) >= 0)
                {
                    break;
                }

                Swap(index, parent);
                index = parent;
                moved = true;
            }

            return moved;
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                var left = index * 2 + 1;
                if (left >= _count)
                {
                    return;
                }

                var right = left + 1;
                var smallest = left;
                if (right < _count && Compare(_heap[right], _heap[left]) < 0)
                {
                    smallest = right;
                }

                if (Compare(_heap[index], _heap[smallest]) <= 0)
                {
                    return;
                }

                Swap(index, smallest);
                index = smallest;
            }
        }

        private int Compare(int a, int b)
        {
            var fA = _fScore[a];
            var fB = _fScore[b];
            if (fA != fB)
            {
                return fA < fB ? -1 : 1;
            }

            return a < b ? -1 : 1;
        }

        private void Swap(int a, int b)
        {
            var temp = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = temp;
            _positions[_heap[a]] = a;
            _positions[_heap[b]] = b;
        }
    }
}
