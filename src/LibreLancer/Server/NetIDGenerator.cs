using System;

namespace LibreLancer.Server
{
    public class NetIDGenerator
    {
        private const int ARRAY_SIZE = 64;

        private IdPool pool;

        public NetIDGenerator()
        {
            pool = new IdPool(ARRAY_SIZE, true);
        }

        public int Allocate()
        {
            pool.TryAllocate(out var id);
            return -(id + 1);
        }

        public void Free(int id)
        {
            if (id >= 0) throw new ArgumentException(nameof(id));
            pool.Free(-id - 1);
        }
    }
}