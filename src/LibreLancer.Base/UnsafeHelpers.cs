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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Text;
using System.Runtime.InteropServices;
namespace LibreLancer
{
    public static class UnsafeHelpers
    {
        public static byte[] CastArray<T>(T[] src) where T : struct
        {
            var sz = Marshal.SizeOf(typeof(T));
            byte[] dst = new byte[src.Length * sz];
            Buffer.BlockCopy(src, 0, dst, 0, dst.Length);
            return dst;
        }
        public static unsafe string PtrToStringUTF8(IntPtr intptr, int maxBytes = int.MaxValue)
        {
            int i = 0;
            var ptr = (byte*)intptr;
            while (ptr[i] != 0) {
                i++;
                if (i >= maxBytes) break;
            }
            var bytes = new byte[i];
            Marshal.Copy(intptr, bytes, 0, i);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
