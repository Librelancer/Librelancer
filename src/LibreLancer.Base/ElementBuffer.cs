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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreLancer
{
    public unsafe class ElementBuffer : IDisposable
    {
        public int IndexCount { get; private set;  }
        public uint Handle;
        public ElementBuffer(int count)
        {
            IndexCount = count;
            Handle = GL.GenBuffer();
        }
        public void SetData(short[] data)
        {
			GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, Handle);
			fixed(short* ptr = data) {
				GL.BufferData (GL.GL_ELEMENT_ARRAY_BUFFER, new IntPtr (data.Length * 2), (IntPtr)ptr, GL.GL_STATIC_DRAW);
			}
        }
        public void SetData(ushort[] data)
        {
			GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, Handle);
			fixed(ushort* ptr = data) {
				GL.BufferData (GL.GL_ELEMENT_ARRAY_BUFFER, new IntPtr (data.Length * 2), (IntPtr)ptr, GL.GL_STATIC_DRAW);
			}
        }
        public void Dispose()
        {
            GL.DeleteBuffer(Handle);
        }
    }
}
