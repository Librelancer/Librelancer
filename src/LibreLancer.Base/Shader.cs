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
        uint programID = 0;
        Dictionary<string, int> progLocations = new Dictionary<string, int>();
		Dictionary<int, object> cachedObjects = new Dictionary<int, object>();
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
        }
		bool NeedUpdate(int loc, object obj)
		{
			if (cachedObjects.ContainsKey (loc)) {
				return !cachedObjects [loc].Equals (obj);
			}
			return true;
		}
		void Update(int loc, object obj)
		{
			if (cachedObjects.ContainsKey (loc)) {
				cachedObjects [loc] = obj;
			} else
				cachedObjects.Add (loc, obj);
		}
        int GetLocation(string name)
        {
            if (!progLocations.ContainsKey(name))
                progLocations[name] = GL.GetUniformLocation(programID, name);
            return progLocations[name];
        }

        public void SetMatrix(string name, ref Matrix4 mat)
        {
            GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			if(NeedUpdate(loc, (object)mat)){
				var handle = GCHandle.Alloc (mat, GCHandleType.Pinned);
				GL.UniformMatrix4fv (GetLocation (name), 1, false, handle.AddrOfPinnedObject());
				handle.Free ();
				Update(loc, mat);
			}
           
        }

        public void SetInteger(string name, int value)
        {
            GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			if (NeedUpdate (loc, value)) {
				GL.Uniform1i (GetLocation (name), value);
				Update (loc, value);
			}
        }

		public void SetInteger(string name, int value, int index)
		{
			GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			if (NeedUpdate (loc + index, value)) {
				GL.Uniform1i (GetLocation (name) + index, value);
				Update (loc + index, value);
			}
		}

        public void SetFloat(string name, float value)
        {
            GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			if (NeedUpdate (loc, value)) {
				GL.Uniform1f (GetLocation (name), value);
				Update (loc, value);
			}
        }

		public void SetFloat(string name, float value, int index)
		{
			GLBind.UseProgram(programID);
			var loc = GetLocation(name);
			if (loc == -1)
				return;
			if (NeedUpdate(loc + index, value))
			{
				GL.Uniform1f(GetLocation(name) + index, value);
				Update(loc + index, value);
			}
		}

        public void SetColor4(string name, Color4 value)
        {
            GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			if (NeedUpdate (loc, value)) {
				GL.Uniform4f (GetLocation (name), value.R, value.G, value.B, value.A);
				Update (loc, value);
			}
        }

		public void SetColor4(string name, Color4 value, int index)
		{
			GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			if (NeedUpdate (loc + index, value)) {
				GL.Uniform4f (GetLocation (name) + index, value.R, value.G, value.B, value.A);
				Update (loc + index, value);
			}
		}

		public void SetVector4(string name, Vector4 value, int index = 0)
		{
			GLBind.UseProgram(programID);
			var loc = GetLocation(name);
			if (loc == -1)
				return;
			if (NeedUpdate(loc + index, value))
			{
				GL.Uniform4f(GetLocation(name) + index, value.X, value.Y, value.Z, value.W);
				Update(loc + index, value);
			}
		}

		public void SetVector3(string name, Vector3 vector, int index = 0)
		{
			GLBind.UseProgram (programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			if (NeedUpdate (loc + index, vector)) {
				GL.Uniform3f (GetLocation (name) + index, vector.X, vector.Y, vector.Z);
				Update (loc + index, vector);
			}
		}

		public void SetVector2(string name, Vector2 vector, int index = 0)
		{
			GLBind.UseProgram(programID);
			var loc = GetLocation(name);
			if (loc == -1)
				return;
			if (NeedUpdate(loc + index, vector))
			{
				GL.Uniform2f(GetLocation(name) + index, vector.X, vector.Y);
				Update(loc + index, vector);
			}
		}

        public void UseProgram()
        {
            GLBind.UseProgram(programID);
        }
    }
}