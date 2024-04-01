// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer.Render
{
	public struct RenderLight
	{
		public LightKind Kind;
		public Vector3 Position;
		public Vector3 Direction;
		public Vector3 Attenuation;
		public Color3f Color;
        public Color3f Ambient;
		public float Range;

		public float Falloff;
		public float Theta;
		public float Phi;

		public override int GetHashCode()
		{
			int hash = 17;
			unchecked
			{
                hash = hash * 23 + (int)Kind * 7;
				hash = hash * 23 + Position.GetHashCode();
				hash = hash * 23 + Direction.GetHashCode();
				hash = hash * 23 + Attenuation.GetHashCode();
				hash = hash * 23 + Color.GetHashCode();
				hash = hash * 23 + Range.GetHashCode();
				hash = hash * 23 + Falloff.GetHashCode();
				hash = hash * 23 + Theta.GetHashCode();
				hash = hash * 23 + Phi.GetHashCode();
			}
			return hash;
		}
	}
}

