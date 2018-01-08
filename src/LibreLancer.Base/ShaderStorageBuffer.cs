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
namespace LibreLancer
{
	public class ShaderStorageBuffer : IDisposable
	{
		public int Size { get; private set; }
		uint ID;
		public ShaderStorageBuffer(int size)
		{
			if (!GLExtensions.Features430)
				throw new PlatformNotSupportedException("Platform does not support Shader Storage Buffer Objects");
			ID = GL.GenBuffer();
			GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, ID);
			GL.BufferData(GL.GL_SHADER_STORAGE_BUFFER, new IntPtr(size), IntPtr.Zero, GL.GL_DYNAMIC_DRAW);
			GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, 0);
		}
		public SSBOHandle Map(bool read = false)
		{
			GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, ID);
			var ptr = GL.MapBuffer(GL.GL_SHADER_STORAGE_BUFFER, (uint)(read ? GL.GL_READ_WRITE : GL.GL_WRITE_ONLY));
			return new SSBOHandle(this, ptr);
		}
		internal void Unmap()
		{
			GL.UnmapBuffer(GL.GL_SHADER_STORAGE_BUFFER);
			GL.BindBuffer(GL.GL_SHADER_STORAGE_BUFFER, 0);
		}

		public void BindIndex(uint index)
		{
			GL.BindBufferBase(GL.GL_SHADER_STORAGE_BUFFER, index, ID);
		}
		public static void UnbindIndex(uint index)
		{
			GL.BindBufferBase(GL.GL_SHADER_STORAGE_BUFFER, index, 0);
		}

		public void Dispose()
		{
			GL.DeleteBuffer(ID);
		}
	}
	public struct SSBOHandle : IDisposable
	{
		ShaderStorageBuffer parent;
		public IntPtr Handle;
		public SSBOHandle(ShaderStorageBuffer p, IntPtr h)
		{
			parent = p;
			Handle = h;
		}
		public void Dispose()
		{
			parent.Unmap();
		}
	}
}
