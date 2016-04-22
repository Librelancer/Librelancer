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
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
	static class GLBind
	{
		static int programBound = -1;
		public static void UseProgram(int prg)
		{
			if (programBound != prg) {
				GL.UseProgram (prg);
				programBound = prg;
			}
		}

		static int bound_vbo = -1;
		static int bound_vao = -1;

		public static void VertexBuffer(int vbo)
		{
			if (bound_vbo != vbo) {
				bound_vbo = vbo;
				GL.BindBuffer (BufferTarget.ArrayBuffer, vbo);
			}
		}
		public static void VertexArray(int vao)
		{
			if (bound_vao != vao) {
				bound_vao = vao;
				GL.BindVertexArray (vao);
			}
		}
	}
}

