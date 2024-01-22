// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics
{
    public class Shader
    {
        public ulong UserTag = 0;
        private IShader impl;

		public Shader(RenderContext context, string vertex_source, string fragment_source, string geometry_source = null)
        {
            impl = context.Backend.CreateShader(vertex_source, fragment_source, geometry_source);
        }


        public int GetLocation(string name) => impl.GetLocation(name);

        public void SetMatrix(int loc, ref Matrix4x4 mat) => impl.SetMatrix(loc, ref mat);

        public void SetMatrix(int loc, IntPtr mat) => impl.SetMatrix(loc, mat);

        public void SetInteger(int loc, int value, int index = 0) => impl.SetInteger(loc, value, index);

        public void SetFloat(int loc, float value, int index = 0) => impl.SetFloat(loc, value, index);

        public void SetColor4(int loc, Color4 value, int index = 0) => impl.SetColor4(loc, value, index);

        public void SetVector4(int loc, Vector4 value, int index = 0) => impl.SetVector4(loc, value, index);

        public unsafe void SetVector4Array(int loc, Vector4* values, int count) =>
            impl.SetVector4Array(loc, values, count);

        public unsafe void SetVector3Array(int loc, Vector3* values, int count) =>
            impl.SetVector3Array(loc, values, count);

        public void SetVector4i(int loc, Vector4i value, int index = 0) =>
            impl.SetVector4i(loc, value, index);

        public void SetVector3(int loc, Vector3 vector, int index = 0) =>
            impl.SetVector3(loc, vector, index);

        public void SetVector2(int loc, Vector2 vector, int index = 0) =>
            impl.SetVector2(loc, vector, index);

        public void UniformBlockBinding(string uniformBlock, int index) =>
            impl.UniformBlockBinding(uniformBlock, index);

        public void UseProgram() =>
            impl.UseProgram();
    }
}
