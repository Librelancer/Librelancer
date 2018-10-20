// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public struct LightBitfield
    {
        public static int Capacity = 128;
        long a;
        long b;
        public bool this[int idx]
        {
            get
            {
                if (idx > 127 || idx < 0)
                    throw new IndexOutOfRangeException();
                if (idx > 63)
                    return (b & (1L << idx - 63)) != 0;
                else
                    return (a & (1L << idx)) != 0;
            }
            set
            {
                if (idx > 127 || idx < 0)
                    throw new IndexOutOfRangeException();
                if (idx > 63)
                {
                    if (value)
                        b |= (1L << (idx - 63));
                    else
                        b &= ~(1L << (idx - 63));
                }
                else
                {
                    if (value)
                        a |= (1L << idx);
                    else
                        a &= ~(1L << idx);
                }
            }
        }
    }
}
