// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;

namespace LibreLancer.ContentEdit
{
    public static unsafe class DefaultTexture
    {
        private static readonly void* _ptr;
        private static readonly int _len;
        public static ReadOnlySpan<byte> Data => new(_ptr, _len);
        static DefaultTexture()
        {
            using var s = (UnmanagedMemoryStream) typeof(DefaultTexture).Assembly.GetManifestResourceStream("LibreLancer.ContentEdit.defaulttexture.dds")!;
            _ptr = (void*)s.PositionPointer;
            _len = checked((int)s.Length);
        }
    }
}
