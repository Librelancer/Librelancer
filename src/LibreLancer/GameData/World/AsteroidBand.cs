// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer.GameData.World
{
	public class AsteroidBand
	{
		public int RenderParts;
		public string Shape;
		public int Height;
		public Vector4 Fade;
		public Color4 ColorShift;
		public float TextureAspect;
		public float OffsetDistance;

        public AsteroidBand Clone() => (AsteroidBand) MemberwiseClone();
    }
}

