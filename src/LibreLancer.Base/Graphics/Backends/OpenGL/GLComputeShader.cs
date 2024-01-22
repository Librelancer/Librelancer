// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    class GLComputeShader : IComputeShader
    {
        uint ID;
        public GLComputeShader(string shaderCode)
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

        public unsafe void UniformMatrix4fv(string name, ref Matrix4x4 mat)
        {
            GLBind.UseProgram(ID);
            fixed (Matrix4x4* ptr = &mat)
            {
                GL.UniformMatrix4fv(GL.GetUniformLocation(ID, name), 1, false, (IntPtr)ptr);
            }
        }

        public void Dispatch(uint groupsX, uint groupsY, uint groupsZ)
        {
            GLBind.UseProgram(ID);
            GL.DispatchCompute(groupsX, groupsY, groupsZ);
        }
    }
}
