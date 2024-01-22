// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics
{
	public class ComputeShader
    {
        private IComputeShader impl;
		public ComputeShader(RenderContext context, string shaderCode)
        {
            impl = context.Backend.CreateComputeShader(shaderCode);
        }

        public void Uniform1i(string name, int i) =>
            impl.Uniform1i(name, i);

        public void Uniform2i(string name, Point pt) =>
            impl.Uniform2i(name, pt);

        public void UniformMatrix4fv(string name, ref Matrix4x4 mat) =>
            impl.UniformMatrix4fv(name, ref mat);

        public void Dispatch(uint groupsX, uint groupsY, uint groupsZ) =>
            impl.Dispatch(groupsX, groupsY, groupsZ);
    }
}
