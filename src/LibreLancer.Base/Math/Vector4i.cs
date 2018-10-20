// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4i
    {
        public int X;
        public int Y;
        public int Z;
        public int W;

        public Vector4i(int x, int y, int z, int w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.X;
                hashCode = (hashCode * 397) ^ this.Y;
                hashCode = (hashCode * 397) ^ this.Z;
                hashCode = (hashCode * 397) ^ this.W;
                return hashCode;
            }
        }
    }
}
