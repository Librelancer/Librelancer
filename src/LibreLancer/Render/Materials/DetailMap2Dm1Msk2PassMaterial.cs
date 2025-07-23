// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Resources;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials
{
	public class DetailMap2Dm1Msk2PassMaterial : RenderMaterial
	{
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct MaterialParameters
        {
            public Color4 Ac;
            public Color4 Dc;
            public float TileRate;
            public float FlipU;
            public float FlipV;
            private float _padding;
        }

        private MaterialParameters parameters;
        public ref Color4 Ac => ref parameters.Ac;
        public ref Color4 Dc => ref parameters.Dc;
        public ref float TileRate => ref parameters.TileRate;
        public int FlipU
        {
            get => (int)parameters.FlipU;
            set => parameters.FlipU = value;
        }

        public int FlipV
        {
            get => (int)parameters.FlipV;
            set => parameters.FlipV = value;
        }

        public string Dm1Sampler;
        public SamplerFlags Dm1Flags;
        public string DtSampler;
        public SamplerFlags DtFlags;

		public DetailMap2Dm1Msk2PassMaterial (ResourceManager library) : base(library) { }

        public override void Use (RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Opaque;

            var sh = AllShaders.DetailMap2Dm1Msk2PassMaterial.Get(rstate.HasFeature(GraphicsFeature.GLES) ? 1U : 0U);
            SetWorld(sh);
            sh.SetUniformBlock(3, ref parameters);
            Vector4 noAnim = new Vector4(0, 0, 1, 1);
            sh.SetUniformBlock(4, ref noAnim);
			BindTexture (rstate ,0, DtSampler, 0, DtFlags);
            BindTexture(rstate, 1, Dm1Sampler, 1, Dm1Flags, ResourceManager.GreyTextureName);
			SetLights(sh, ref lights, rstate.FrameNumber);
            rstate.Shader = sh;
        }

		public override bool IsTransparent
		{
			get
			{
				return false;
			}
		}
	}
}

