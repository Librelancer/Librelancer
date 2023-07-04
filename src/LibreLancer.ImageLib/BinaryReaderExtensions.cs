// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
namespace LibreLancer
{
    static class BinaryReaderExtensions
    {
        public static unsafe T ReadStruct<T>(this BinaryReader reader) where T : unmanaged
        {
            T value = new T();
            Span<byte> bytes = new Span<byte>(&value, sizeof(T));
            if (reader.Read(bytes) != sizeof(T))
                throw new EndOfStreamException();
            return value;
        }
    }
}

