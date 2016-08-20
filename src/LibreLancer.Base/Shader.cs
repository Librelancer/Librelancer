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
using System.Runtime.InteropServices;
namespace LibreLancer
{
    public class Shader
    {
        const int MAX_UNIFORM_LOC = 128;
        uint programID = 0;
        Dictionary<string, int> progLocations = new Dictionary<string, int>();
        int[] cachedObjects = new int[MAX_UNIFORM_LOC];
		public Shader(string vertex_source, string fragment_source, string geometry_source = null)
        {
			var vertexHandle = GL.CreateShader (GL.GL_VERTEX_SHADER);
			var fragmentHandle = GL.CreateShader (GL.GL_FRAGMENT_SHADER);
            GL.ShaderSource(vertexHandle, vertex_source);
            GL.ShaderSource(fragmentHandle, fragment_source);
            GL.CompileShader(vertexHandle);
			int status;
			GL.GetShaderiv (vertexHandle, GL.GL_COMPILE_STATUS, out status);
			if (status == 0) {
				Console.WriteLine (GL.GetShaderInfoLog (vertexHandle));
				throw new Exception ("Vertex shader compilation failed");
			}
            GL.CompileShader(fragmentHandle);
			GL.GetShaderiv (fragmentHandle, GL.GL_COMPILE_STATUS, out status);
			if (status == 0) {
				Console.WriteLine (GL.GetShaderInfoLog (fragmentHandle));

				throw new Exception ("Fragment shader compilation failed");
			}
            programID = GL.CreateProgram();
			if (geometry_source != null) {
				var geometryHandle = GL.CreateShader (GL.GL_GEOMETRY_SHADER);
				GL.ShaderSource (geometryHandle, geometry_source);
				GL.CompileShader (geometryHandle);
				GL.GetShaderiv (geometryHandle, GL.GL_COMPILE_STATUS, out status);
				if (status == 0) {
					Console.WriteLine (GL.GetShaderInfoLog (geometryHandle));
					throw new Exception ("Geometry shader compilation failed");
				}
				GL.AttachShader (programID, geometryHandle);
			}
            GL.AttachShader(programID, vertexHandle);
            GL.AttachShader(programID, fragmentHandle);
			//Attributes
            GL.BindAttribLocation(programID, VertexSlots.Position, "vertex_position");
            GL.BindAttribLocation(programID, VertexSlots.Normal, "vertex_normal");
            GL.BindAttribLocation(programID, VertexSlots.Color, "vertex_color");
            GL.BindAttribLocation(programID, VertexSlots.Texture1, "vertex_texture1");
			GL.BindAttribLocation (programID, VertexSlots.Texture2, "vertex_texture2");
			GL.BindAttribLocation (programID, VertexSlots.Texture3, "vertex_texture3");
			GL.BindAttribLocation (programID, VertexSlots.Texture4, "vertex_texture4");
			GL.BindAttribLocation (programID, VertexSlots.Size, "vertex_size");
			GL.BindAttribLocation (programID, VertexSlots.Angle, "vertex_angle");
			//Fragment Outputs
			GL.BindFragDataLocation(programID, 0, "out_color0");
			GL.BindFragDataLocation(programID, 1, "out_color1");
			GL.BindFragDataLocation(programID, 2, "out_color2");
			GL.BindFragDataLocation(programID, 3, "out_color3");

            GL.LinkProgram(programID);
			GL.GetProgramiv (programID, GL.GL_LINK_STATUS, out status);
			if (status == 0) {
				Console.WriteLine (GL.GetProgramInfoLog (programID));
				throw new Exception ("Program link failed");
			}

			viewProjectionLoc = GetLocation("ViewProjection");
			viewLoc = GetLocation("View");
			worldLoc = GetLocation("World");
        }
		int viewProjectionLoc = 0; 
		int viewLoc = 0;
		int worldLoc = 0;

		public int UserTag = 0;
		bool NeedUpdate(int loc, int hash)
		{
            return cachedObjects[loc] != hash;
		}
        int GetLocation(string name)
        {
            int loc;
            if(!progLocations.TryGetValue(name, out loc))
            {
                loc = GL.GetUniformLocation(programID, name);
                progLocations[name] = loc;
            }
            return loc;
        }

		void SetMatrixInternal(int loc, ref Matrix4 mat)
		{
			//var hash = mat.GetHashCode();
			//if (NeedUpdate(loc, hash))
			//{
			//var handle = GCHandle.Alloc(mat, GCHandleType.Pinned);
			GL.UniformMatrix4fv(loc, 1, false, ref mat);
				//handle.Free();
			//	cachedObjects[loc] = hash;
			//}
		}

        public void SetMatrix(string name, ref Matrix4 mat)
        {
            GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			SetMatrixInternal(loc, ref mat);
        }

		public void SetWorld(ref Matrix4 mat)
		{
			GLBind.UseProgram(programID);
            SetMatrixInternal(worldLoc, ref mat);
		}
		public void SetView(ref Matrix4 mat)
		{
			GLBind.UseProgram(programID);
			SetMatrixInternal(viewLoc, ref mat);
		}
		public void SetViewProjection(ref Matrix4 mat)
		{
			GLBind.UseProgram(programID);
			SetMatrixInternal(viewProjectionLoc, ref mat);
		}

		public void SetInteger(string name, int value, int index = 0)
		{
			GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			if (NeedUpdate (loc + index, value)) {
				GL.Uniform1i (loc + index, value);
                cachedObjects[loc + index] = value;
			}
		}
        [StructLayout(LayoutKind.Explicit)]
        struct Float2Int
        {
            [FieldOffset(0)]
            public int i;
            [FieldOffset(0)]
            public float f;
        }
        public void SetFloat(string name, float value, int index = 0)
        {
            GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
            var ibits = (new Float2Int() { f = value }).i;
			if (NeedUpdate (loc + index, ibits)) {
				GL.Uniform1f (loc + index, value);
                cachedObjects[loc + index] = ibits;
			}
        }

		public void SetColor4(string name, Color4 value, int index = 0)
		{
			GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
            var hash = value.GetHashCode();
			if (NeedUpdate (loc + index, hash)) {
				GL.Uniform4f (loc + index, value.R, value.G, value.B, value.A);
                cachedObjects[loc + index] = hash;
			}
		}

		public void SetVector4(string name, Vector4 value, int index = 0)
		{
			GLBind.UseProgram(programID);
			var loc = GetLocation(name);
			if (loc == -1)
				return;
            var hash = value.GetHashCode();
			if (NeedUpdate(loc + index, hash))
			{
				GL.Uniform4f(loc + index, value.X, value.Y, value.Z, value.W);
				cachedObjects[loc + index] = hash;
			}
		}

		public void SetVector3(string name, Vector3 vector, int index = 0)
		{
			GLBind.UseProgram (programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
            var hash = vector.GetHashCode();
			if (NeedUpdate (loc + index, hash)) {
				GL.Uniform3f (loc + index, vector.X, vector.Y, vector.Z);
                cachedObjects[loc + index] = hash;
			}
		}

		public void SetVector2(string name, Vector2 vector, int index = 0)
		{
			GLBind.UseProgram(programID);
			var loc = GetLocation(name);
			if (loc == -1)
				return;
            var hash = vector.GetHashCode();
			if (NeedUpdate(loc + index, hash))
			{
				GL.Uniform2f(loc + index, vector.X, vector.Y);
                cachedObjects[loc + index] = hash;
			}
		}

        public void UseProgram()
        {
            GLBind.UseProgram(programID);
        }
    }
}