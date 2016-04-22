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
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK;

namespace LibreLancer
{
    public class Shader
    {
        int programID = 0;
        Dictionary<string, int> progLocations = new Dictionary<string, int>();
		Dictionary<int, object> cachedObjects = new Dictionary<int, object>();
		public Shader(string vertex_source, string fragment_source, string geometry_source = null)
        {
            var vertexHandle = GL.CreateShader(ShaderType.VertexShader);
            var fragmentHandle = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(vertexHandle, vertex_source);
            GL.ShaderSource(fragmentHandle, fragment_source);
            GL.CompileShader(vertexHandle);
			int status;
			GL.GetShader (vertexHandle, ShaderParameter.CompileStatus, out status);
			if (status == 0) {
				Console.WriteLine (GL.GetShaderInfoLog (vertexHandle));
				throw new Exception ("Vertex shader compilation failed");
			}
            GL.CompileShader(fragmentHandle);
			GL.GetShader (fragmentHandle, ShaderParameter.CompileStatus, out status);
			if (status == 0) {
				Console.WriteLine (GL.GetShaderInfoLog (fragmentHandle));

				throw new Exception ("Fragment shader compilation failed");
			}
            programID = GL.CreateProgram();
			if (geometry_source != null) {
				var geometryHandle = GL.CreateShader (ShaderType.GeometryShader);
				GL.ShaderSource (geometryHandle, geometry_source);
				GL.CompileShader (geometryHandle);
				GL.GetShader (geometryHandle, ShaderParameter.CompileStatus, out status);
				if (status == 0) {
					Console.WriteLine (GL.GetShaderInfoLog (geometryHandle));
					throw new Exception ("Geometry shader compilation failed");
				}
				GL.AttachShader (programID, geometryHandle);
			}
            GL.AttachShader(programID, vertexHandle);
            GL.AttachShader(programID, fragmentHandle);
            GL.BindAttribLocation(programID, VertexSlots.Position, "vertex_position");
            GL.BindAttribLocation(programID, VertexSlots.Normal, "vertex_normal");
            GL.BindAttribLocation(programID, VertexSlots.Color, "vertex_color");
            GL.BindAttribLocation(programID, VertexSlots.Texture1, "vertex_texture1");
			GL.BindAttribLocation (programID, VertexSlots.Texture2, "vertex_texture2");
			GL.BindAttribLocation (programID, VertexSlots.Texture3, "vertex_texture3");
			GL.BindAttribLocation (programID, VertexSlots.Texture4, "vertex_texture4");
			GL.BindAttribLocation (programID, VertexSlots.Size, "vertex_size");
			GL.BindAttribLocation (programID, VertexSlots.Angle, "vertex_angle");

            GL.LinkProgram(programID);
			GL.GetProgram (programID, GetProgramParameterName.LinkStatus, out status);
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
				GL.UniformMatrix4(GetLocation(name), false, ref mat);
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
				GL.Uniform1 (GetLocation (name), value);
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
				GL.Uniform1 (GetLocation (name) + index, value);
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
				GL.Uniform1 (GetLocation (name), value);
				Update (loc, value);
			}
        }

        public void SetColor4(string name, Color4 value)
        {
            GLBind.UseProgram(programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			if (NeedUpdate (loc, value)) {
				GL.Uniform4 (GetLocation (name), value);
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
				GL.Uniform4 (GetLocation (name) + index, value);
				Update (loc + index, value);
			}
		}

		public void SetVector3(string name, Vector3 vector, int index = 0)
		{
			GLBind.UseProgram (programID);
			var loc = GetLocation (name);
			if (loc == -1)
				return;
			if (NeedUpdate (loc + index, vector)) {
				GL.Uniform3 (GetLocation (name) + index, vector);
				Update (loc + index, vector);
			}
		}
        public void UseProgram()
        {
            GLBind.UseProgram(programID);
        }
    }
}