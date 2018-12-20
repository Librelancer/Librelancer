// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
namespace LibreLancer
{
    public static class BinaryReaderExtensions
    {
        static Dictionary<Type, int> sizes = new Dictionary<Type, int>();
        public static T ReadStruct<T>(this BinaryReader reader) where T : struct
        {
            //cache the sizes since Marshal.SizeOf doesn't
            var type = typeof(T);
            if (!sizes.ContainsKey(type))
            {
                sizes.Add(type, Marshal.SizeOf(type));
            }
            // Read in a byte array
            byte[] bytes = reader.ReadBytes(sizes[type]);

            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), type);
            handle.Free();

            return theStructure;
        }
    }
}

