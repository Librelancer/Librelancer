// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer.Render
{
    public unsafe class WorldMatrixBuffer : IDisposable
    {
        private int MaxMatrices = 131_000; //approx 8MiB
        private uint ticks;
        private uint currentIndex = 0;
        public void Reset()
        {
            ticks++;
            currentIndex = 2;
        }
        public WorldMatrixBuffer()
        {
            buffer = (Matrix4x4*)Marshal.AllocHGlobal(MaxMatrices * 64);
            buffer[0] = Matrix4x4.Identity;
            buffer[1] = Matrix4x4.Identity;
            ticks = (uint)Environment.TickCount64;
            Reset();
            identity.Source = buffer;
            identity.ID = UInt64.MaxValue;
        }

        private WorldMatrixHandle identity;
        public WorldMatrixHandle Identity => identity;
        private Matrix4x4* buffer;
        public WorldMatrixHandle SubmitMatrix(ref Matrix4x4 world)
        {
            if (currentIndex + 2 >= MaxMatrices) {
                throw new Exception("Too many draws this frame");
            }
            var id = (ulong) ((ulong) ticks << 32 | (ulong) currentIndex);
            var idx = currentIndex;
            buffer[currentIndex++] = world;
            Matrix4x4.Invert(world, out var normal);
            buffer[currentIndex++] = Matrix4x4.Transpose(normal);
            return new WorldMatrixHandle()
            {
                Source = &buffer[idx],
                ID = id
            };
        }
        public void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr)buffer);
        }
    }
    public unsafe struct WorldMatrixHandle
    {
        public Matrix4x4* Source;
        public ulong ID;
    }
}