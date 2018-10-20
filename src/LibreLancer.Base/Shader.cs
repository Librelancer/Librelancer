// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
			GL.BindAttribLocation (programID, VertexSlots.Dimensions, "vertex_dimensions");
			GL.BindAttribLocation (programID, VertexSlots.Right, "vertex_right");
			GL.BindAttribLocation (programID, VertexSlots.Up, "vertex_up");
			GL.BindAttribLocation(programID, VertexSlots.BoneFirst, "vertex_bonefirst");
			GL.BindAttribLocation(programID, VertexSlots.BoneCount, "vertex_bonecount");
			//Fragment Outputs

            GL.LinkProgram(programID);
			GL.GetProgramiv (programID, GL.GL_LINK_STATUS, out status);
			if (status == 0) {
				Console.WriteLine (GL.GetProgramInfoLog (programID));
				throw new Exception ("Program link failed");
			}
        }

		public int UserTag = 0;
		bool NeedUpdate(int loc, int hash)
		{
            return cachedObjects[loc] != hash;
		}

		public int GetLocation(string name)
		{
			return GL.GetUniformLocation(programID, name);
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

        public void SetMatrix(int loc, ref Matrix4 mat)
        {
            GLBind.UseProgram(programID);
			SetMatrixInternal(loc, ref mat);
        }

		public void SetInteger(int loc, int value, int index = 0)
		{
			GLBind.UseProgram(programID);
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
        public void SetFloat(int loc, float value, int index = 0)
        {
            GLBind.UseProgram(programID);
            var ibits = (new Float2Int() { f = value }).i;
			if (NeedUpdate (loc + index, ibits)) {
				GL.Uniform1f (loc + index, value);
                cachedObjects[loc + index] = ibits;
			}
        }

		public void SetColor4(int loc, Color4 value, int index = 0)
		{
			GLBind.UseProgram(programID);
            var hash = value.GetHashCode();
			if (NeedUpdate (loc + index, hash)) {
				GL.Uniform4f (loc + index, value.R, value.G, value.B, value.A);
                cachedObjects[loc + index] = hash;
			}
		}

		public void SetVector4(int loc, Vector4 value, int index = 0)
		{
			GLBind.UseProgram(programID);
            var hash = value.GetHashCode();
			if (NeedUpdate(loc + index, hash))
			{
				GL.Uniform4f(loc + index, value.X, value.Y, value.Z, value.W);
				cachedObjects[loc + index] = hash;
			}
		}

        public void SetVector4i(int loc, Vector4i value, int index = 0)
        {
            GLBind.UseProgram(programID);
            var hash = value.GetHashCode();
            if (NeedUpdate(loc + index, hash))
            {
                GL.Uniform4i(loc + index, value.X, value.Y, value.Z, value.W);
                cachedObjects[loc + index] = hash;
            }
        }

		public void SetVector3(int loc, Vector3 vector, int index = 0)
		{
			GLBind.UseProgram (programID);
            var hash = vector.GetHashCode();
			if (NeedUpdate (loc + index, hash)) {
				GL.Uniform3f (loc + index, vector.X, vector.Y, vector.Z);
                cachedObjects[loc + index] = hash;
			}
		}

		public void SetVector2(int loc, Vector2 vector, int index = 0)
		{
			GLBind.UseProgram(programID);
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