// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Data.GameData;

namespace LibreLancer.Render
{
	public struct Lighting
	{
		public const int MAX_LIGHTS = 9;
		public static Lighting Empty = new Lighting() { Enabled = false };
        byte _enabled;
        public bool Enabled
        {
            get { return _enabled == 1; }
            set { _enabled = value ? (byte)1 : (byte)0; }
        }
        public Color3f Ambient;
        public LightsArray Lights;
		public FogModes FogMode;
        public Color3f FogColor;
        public Vector2 FogRange;
		public int NumberOfTilesX;

		public static Lighting Create()
        {
            return new Lighting
            {
                Enabled = true,
                FogColor = Color3f.White,
                FogMode = FogModes.None,
                Ambient = Color3f.Black
            };
        }
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct LightsArray
        {
            public BitArray128 SourceEnabled;
            public SystemLighting SourceLighting;
            public int NebulaCount;
            public RenderLight Nebula0;
        }
	}
}

