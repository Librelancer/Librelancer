/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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

