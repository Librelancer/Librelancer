using System;
using System.Collections.Generic;
using System.Numerics;
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

        public IEnumerable<int> GetAllocated()
        {
            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (bits[i] == uint.MaxValue) continue;
                int tzcnt = BitOperations.TrailingZeroCount(bits[i]);
                for (int j = 0; j < tzcnt; j++)
                    yield return (i << 5) + j;
                for(int j = tzcnt; j < 32; j++)
                    if ((bits[i] & (1U << j)) == 0) yield return (i << 5) + j;
            }
        }

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