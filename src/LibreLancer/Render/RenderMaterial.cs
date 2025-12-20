// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using LibreLancer.Data.GameData;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Resources;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render
{
	public abstract class RenderMaterial
    {
        private static int _key = 0;

        public int Key { get; private set; }

        protected RenderMaterial(ResourceManager library)
        {
            Library = library;
            Key = Interlocked.Increment(ref _key);
        }

        public static bool VertexLighting = false;
		public MaterialAnim MaterialAnim;
		public WorldMatrixHandle World = new WorldMatrixHandle();
		public ResourceManager Library;
		public bool Fade = false;
		public float FadeNear = 0;
		public float FadeFar = 0;
        public StorageBuffer Bones;
        public int BufferOffset;
        public abstract void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData);
		public abstract bool IsTransparent { get; }
        public virtual bool DisableCull {  get { return false; } }
        public bool DoubleSided = false;
		Texture2D[] textures = new Texture2D[8];
		bool[] loaded = new bool[8];


        [StructLayout(LayoutKind.Sequential, Pack = 1)]

        struct PackedLight
        {
            public Vector3 Position;
            public float Type;
            public Color3f Diffuse;
            public float Range;
            public Vector3 Attentuation;
            private float _padding1;
            public Vector3 Direction;
            public float Spotlight;
            public float Falloff;
            public float Theta;
            public float Phi;
            private float _padding2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ShaderLighting
        {
            public Vector2 FogRange;
            public float UseLighting;
            public float FogMode;
            public Color3f AmbientColor;
            public float LightCount;
            public Color3f FogColor;
            public float _padding;
            public PackedLight Light0;
            public PackedLight Light1;
            public PackedLight Light2;
            public PackedLight Light3;
            public PackedLight Light4;
            public PackedLight Light5;
            public PackedLight Light6;
            public PackedLight Light7;
            public PackedLight Light8;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct WorldBuffer
        {
            public Matrix4x4 WorldMatrix;
            public Matrix4x4 NormalMatrix;
        }

        protected unsafe void SetWorld(Shader shader, Matrix4x4 world, Matrix4x4 normal)
        {
            shader.UniformBlockTag(0) = ulong.MaxValue;
            var b = new WorldBuffer() { WorldMatrix = world, NormalMatrix = normal };
            shader.SetUniformBlock(0, ref b);
        }

        protected unsafe void SetWorld(Shader shader)
        {
            if (World.Source == (Matrix4x4*)0)
            {
                SetWorld(shader, Matrix4x4.Identity, Matrix4x4.Identity);
            }
            else if (shader.HasUniformBlock(0) &&
                     World.ID == ulong.MaxValue || shader.UniformBlockTag(0) != World.ID)
            {
                shader.SetUniformBlock(0, ref Unsafe.AsRef<WorldBuffer>(World.Source), true);
                shader.UniformBlockTag(0) = World.ID;
            }
        }

        public static unsafe void SetLights(Shader shader, ref Lighting lighting, long frameNumber)
		{
            if (!lighting.Enabled)
            {
                var disable = Vector4.Zero;
                shader.SetUniformBlock(2, ref disable, false, 16);
                return;
            }

            var data = new ShaderLighting
            {
                UseLighting = 1,
                //fog
                FogMode = (float)lighting.FogMode,
                FogRange = lighting.FogRange,
                FogColor = lighting.FogColor,
                AmbientColor = lighting.Ambient
            };

            int lt = 0;
            var lights = new Span<PackedLight>(&data.Light0, 9);
            for (int i = 0; i < lighting.Lights.SourceLighting.Lights.Count; i++)
            {
                if (!lighting.Lights.SourceEnabled[i])
                    continue;
                var src = lighting.Lights.SourceLighting.Lights[i].Light;
                lights[lt].Position = src.Position;
                lights[lt].Attentuation = src.Attenuation;
                lights[lt].Direction = src.Direction;
                lights[lt].Diffuse = src.Color;
                lights[lt].Range = src.Range;
                if (src.Kind == LightKind.Spotlight)
                {
                    lights[lt].Spotlight = 1;
                    lights[lt].Theta =  MathF.Cos(src.Theta * 0.5f);
                    lights[lt].Phi = MathF.Cos(src.Phi * 0.5f);
                    lights[lt].Falloff = src.Falloff;
                }
                if (src.Kind == LightKind.Point || src.Kind == LightKind.Spotlight)
                    lights[lt].Type = 1;
                else if (src.Kind == LightKind.PointAttenCurve)
                    lights[lt].Type = 2;
                lt++;
                if(lt >= lights.Length) break;
            }

            data.LightCount = lt;
            int szCount = 3 * sizeof(Vector4) + //header
                          lt * sizeof(PackedLight); //lights
            shader.SetUniformBlock<ShaderLighting>(2, ref data, false, szCount);
        }

		protected Texture2D GetTexture(int cacheidx, string tex)
		{
			if (tex == null)
				return (Texture2D)Library.FindTexture(ResourceManager.NullTextureName);
			if (textures[cacheidx] == null)
				textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
			var tex2d = textures[cacheidx];
            if (tex2d == null) return tex2d;
			if (tex2d.IsDisposed)
				tex2d = textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
			return textures[cacheidx];
		}

        protected void SetTextureCoordinates(Shader shader, SamplerFlags t0, SamplerFlags t1 = 0, SamplerFlags t2 = 0, SamplerFlags t3 = 0)
        {
            Vector4i flags = new(
                (t0 & SamplerFlags.SecondUV) == SamplerFlags.SecondUV ? 1 : 0,
                (t1 & SamplerFlags.SecondUV) == SamplerFlags.SecondUV ? 1 : 0,
                (t2 & SamplerFlags.SecondUV) == SamplerFlags.SecondUV ? 1 : 0,
                (t3 & SamplerFlags.SecondUV) == SamplerFlags.SecondUV ? 1 : 0
            );
            if (shader.HasUniformBlock(5))
            {
                shader.SetUniformBlock(5, ref flags);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Flags2
        {
            public Vector4i A;
            public Vector4i B;
        }
        protected void SetTextureCoordinates(Shader shader, SamplerFlags t0, SamplerFlags t1, SamplerFlags t2, SamplerFlags t3, SamplerFlags t4, SamplerFlags t5 = 0)
        {
            var f2 = new Flags2();
            f2.A = new(
                (t0 & SamplerFlags.SecondUV) == SamplerFlags.SecondUV ? 1 : 0,
                (t1 & SamplerFlags.SecondUV) == SamplerFlags.SecondUV ? 1 : 0,
                (t2 & SamplerFlags.SecondUV) == SamplerFlags.SecondUV ? 1 : 0,
                (t3 & SamplerFlags.SecondUV) == SamplerFlags.SecondUV ? 1 : 0
            );
            f2.B = new(
                (t4 & SamplerFlags.SecondUV) == SamplerFlags.SecondUV ? 1 : 0,
                (t5 & SamplerFlags.SecondUV) == SamplerFlags.SecondUV ? 1 : 0,
                0, 0
            );

            if (shader.HasUniformBlock(5))
            {
                shader.SetUniformBlock(5, ref f2);
            }
        }

		protected void BindTexture(RenderContext rstate, int cacheidx, string tex, int unit, SamplerFlags flags, string nullName = null)
		{
			if (tex == null)
			{
                tex = nullName ?? ResourceManager.NullTextureName;
			}
			if (textures[cacheidx] == null || !loaded[cacheidx])
				textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
			if (textures[cacheidx] == null)
			{
				textures[cacheidx] = (Texture2D)Library.FindTexture(ResourceManager.NullTextureName);
				loaded[cacheidx] = false;
			}
			else
				loaded[cacheidx] = true;
			var tex2d = textures[cacheidx];
			if (tex2d.IsDisposed)
				tex2d = textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
            if (tex2d == null)
                tex2d = (Texture2D)Library.FindTexture(ResourceManager.NullTextureName);
			tex2d.BindTo(unit);
			tex2d.SetFiltering(rstate.PreferredFilterLevel);
			if ((flags & SamplerFlags.ClampToEdgeU) == SamplerFlags.ClampToEdgeU)
			{
				tex2d.SetWrapModeS(WrapMode.ClampToEdge);
			}
			if ((flags & SamplerFlags.ClampToEdgeV) == SamplerFlags.ClampToEdgeV)
			{
				tex2d.SetWrapModeT(WrapMode.ClampToEdge);
			}
			if ((flags & SamplerFlags.MirrorRepeatU) == SamplerFlags.MirrorRepeatU)
			{
				tex2d.SetWrapModeS(WrapMode.MirroredRepeat);
			}
			if ((flags & SamplerFlags.MirrorRepeatV) == SamplerFlags.MirrorRepeatV)
			{
				tex2d.SetWrapModeT(WrapMode.MirroredRepeat);
			}

		}
	}
}

