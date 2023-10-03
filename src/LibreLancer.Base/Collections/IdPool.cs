using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LibreLancer
{
    public class IdPool
    {
        private uint[] bits;
        private bool canExpand;

        public int Count { get; private set; }

        public IdPool(int arraySize, bool canExpand)
        {
            bits = new uint[arraySize];
            for (int i = 0; i < bits.Length; i++)
                bits[i] = uint.MaxValue;
            this.canExpand = canExpand;
        }

        private int maxIndex = 0;
        private int minIndex = 0;
        private int minFree = 0;

        public bool TryAllocate(out int allocated)
        {
            for (int i = minFree; i < bits.Length; i++)
            {
                if (bits[i] != 0)
                {
                    var index = BitOperations.TrailingZeroCount(bits[i]);
                    bits[i] &= ~(1U << index);
                    allocated = (i << 5) + index;
                    maxIndex = Math.Max(maxIndex, i);
                    minIndex = Math.Min(minIndex, i);
                    minFree = bits[i] == 0 ? i + 1 : i;
                    Count++;
                    return true;
                }
            }
            if (canExpand)
            {
                var i = bits.Length;
                Array.Resize(ref bits, bits.Length * 2);
                bits[i] = 0xfffffffe;
                for (int j = i + 1; j < bits.Length; j++) bits[j] = uint.MaxValue;
                allocated = i << 5;
                maxIndex = Math.Max(maxIndex, i);
                minIndex = Math.Min(minIndex, i);
                Count++;
                return true;
            }
            allocated = -1;
            return false;
        }

        public struct AllocatedEnumerator : IEnumerator<int>
        {
            private IdPool pool;
            private int i;
            private int j;
            private int tzcnt;
            private bool innerLoop;
            public AllocatedEnumerator(IdPool pool)
            {
                this.pool = pool;
                this.j = this.tzcnt = 0;
                this.innerLoop = false;
                this.i = pool.minIndex - 1;
            }
            public bool MoveNext()
            {
                while (true)
                {
                    if (innerLoop)
                    {
                        if (j < tzcnt)
                        {
                            Current = (i << 5) + j;
                            j++;
                            return true;
                        }
                        while (j < 32)
                        {
                            if ((pool.bits[i] & (1U << j)) == 0)
                            {
                                Current = (i << 5) + j;
                                j++;
                                return true;
                            }
                            j++;
                        }
                    }
                    i++;
                    while (i <= pool.maxIndex && pool.bits[i] == uint.MaxValue)
                        i++;
                    if (i > pool.maxIndex)
                        return false;
                    tzcnt = BitOperations.TrailingZeroCount(pool.bits[i]);
                    innerLoop = true;
                    if (tzcnt > 0)
                    {
                        Current = (i << 5);
                        j = 1;
                        return true;
                    }
                }
            }

            public void Reset()
            {
                j = 0;
                i = pool.minIndex - 1;
            }

            public int Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose() => Reset();
        }

        public StructEnumerable<int, AllocatedEnumerator> GetAllocated() =>
            new (new AllocatedEnumerator(this));

        public void Free(int id)
        {
            if (id < 0 || id >= bits.Length * 32) throw new IndexOutOfRangeException();
            Count--;
            int index = id >> 5;
            bits[index] |= 1U << (id & 0x1f);
            minFree = Math.Min(minFree, index);
            if (bits[index] == uint.MaxValue) {
                if (maxIndex == index) maxIndex--;
                if (minIndex == index) minIndex++;
                if (maxIndex <= 0) maxIndex = 0;
                if (minIndex <= 0) minIndex = 0;
            }
        }
    }
}
