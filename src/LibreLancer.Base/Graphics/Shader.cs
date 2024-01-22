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
        internal IShader Backing;

		public Shader(RenderContext context, string vertex_source, string fragment_source, string geometry_source = null)
        {
            Backing = context.Backend.CreateShader(vertex_source, fragment_source, geometry_source);
        }


        public int GetLocation(string name) => Backing.GetLocation(name);

        public void SetMatrix(int loc, ref Matrix4x4 mat) => Backing.SetMatrix(loc, ref mat);

        public void SetMatrix(int loc, IntPtr mat) => Backing.SetMatrix(loc, mat);

        public void SetInteger(int loc, int value, int index = 0) => Backing.SetInteger(loc, value, index);

        public void SetFloat(int loc, float value, int index = 0) => Backing.SetFloat(loc, value, index);

        public void SetColor4(int loc, Color4 value, int index = 0) => Backing.SetColor4(loc, value, index);

        public void SetVector4(int loc, Vector4 value, int index = 0) => Backing.SetVector4(loc, value, index);

        public unsafe void SetVector4Array(int loc, Vector4* values, int count) =>
            Backing.SetVector4Array(loc, values, count);

        public unsafe void SetVector3Array(int loc, Vector3* values, int count) =>
            Backing.SetVector3Array(loc, values, count);

        public void SetVector4i(int loc, Vector4i value, int index = 0) =>
            Backing.SetVector4i(loc, value, index);

        public void SetVector3(int loc, Vector3 vector, int index = 0) =>
            Backing.SetVector3(loc, vector, index);

        public void SetVector2(int loc, Vector2 vector, int index = 0) =>
            Backing.SetVector2(loc, vector, index);

        public void UniformBlockBinding(string uniformBlock, int index) =>
            Backing.UniformBlockBinding(uniformBlock, index);
    }
}
