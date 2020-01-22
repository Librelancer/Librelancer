// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.IO;

namespace LibreLancer
{
    public static class ArraySegmentHelper
    {
        public static MemoryStream GetReadStream(this ArraySegment<byte> segment)
        {
            return new MemoryStream(segment.Array, segment.Offset, segment.Count, false);
        }

        public static T AtIndex<T>(this ArraySegment<T> segment, int index)
        {
            if(index < 0 || index >= segment.Count) throw new IndexOutOfRangeException();
            return segment.Array[segment.Offset + index];
        }
    }
}