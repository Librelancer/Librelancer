// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace LibreLancer
{
	public struct Lighting
	{
		public const int MAX_LIGHTS = 9;
		public static Lighting Empty = new Lighting() { Enabled = false };
        public bool Enabled;
        public Color4 Ambient;
        public LightsArray Lights;
		public FogModes FogMode;
		public float FogDensity;
        public Color4 FogColor;
        public Vector2 FogRange;
		public int NumberOfTilesX;

		public static Lighting Create()
        {
            return new Lighting
            {
                Enabled = true,
                FogColor = Color4.White,
                FogMode = FogModes.None,
                Ambient = Color4.Black
            };
        }
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct LightsArray
        {
            public LightBitfield SourceEnabled;
            public SystemLighting SourceLighting;
            public int NebulaCount;
            public RenderLight Nebula0;
        }
	}
}

