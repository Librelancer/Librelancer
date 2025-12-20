// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Data.GameData.World
{
	public class AsteroidBand : IDataEquatable<AsteroidBand>
	{
		public int RenderParts;
		public string Shape;
		public int Height;
		public Vector4 Fade;
		public Color4 ColorShift;
		public float TextureAspect;
		public float OffsetDistance;

        public AsteroidBand Clone() => (AsteroidBand) MemberwiseClone();
        public bool DataEquals(AsteroidBand other) =>
            RenderParts == other.RenderParts &&
            string.Equals(Shape, other.Shape, StringComparison.OrdinalIgnoreCase) &&
            Height == other.Height &&
            Fade == other.Fade &&
            ColorShift == other.ColorShift &&
            // ReSharper disable CompareOfFloatsByEqualityOperator
            TextureAspect  == other.TextureAspect &&
            OffsetDistance == other.OffsetDistance;
    }
}

