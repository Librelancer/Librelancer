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
	public class ComputeShader
	{
		uint ID;
		public ComputeShader(string shaderCode)
		{
			if (!GLExtensions.ComputeShaders)
				throw new PlatformNotSupportedException("Compute Shaders not supported by this device");
			uint shd = GL.CreateShader(GL.GL_COMPUTE_SHADER);
			GL.ShaderSource(shd, shaderCode);
			GL.CompileShader(shd);
			int status;
			GL.GetShaderiv(shd, GL.GL_COMPILE_STATUS, out status);
			if (status == 0) {
				Console.WriteLine(GL.GetShaderInfoLog(shd));
				throw new Exception("Compute shader compilation failed");
			}
			ID = GL.CreateProgram();
			GL.AttachShader(ID, shd);
			GL.LinkProgram(ID);
			GL.GetProgramiv(ID, GL.GL_LINK_STATUS, out status);
			if (status == 0) {
				Console.WriteLine(GL.GetProgramInfoLog(ID));
				throw new Exception("Compute program link failed");
			}
		}

		public void Uniform1i(string name, int i)
		{
			GLBind.UseProgram(ID);
			GL.Uniform1i(GL.GetUniformLocation(ID, name), i);
		}

		public void Uniform2i(string name, Point pt)
		{
			GLBind.UseProgram(ID);
			GL.Uniform2i(GL.GetUniformLocation(ID, name), pt.X, pt.Y);
		}

		public void UniformMatrix4fv(string name, ref Matrix4 mat)
		{
			GLBind.UseProgram(ID);
			GL.UniformMatrix4fv(GL.GetUniformLocation(ID, name), 1, false, ref mat);
		}

		public void Dispatch(uint groupsX, uint groupsY, uint groupsZ)
		{
			GLBind.UseProgram(ID);
			GL.DispatchCompute(groupsX, groupsY, groupsZ);
		}
	}
}
