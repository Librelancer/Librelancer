// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.GameData.World
{
	public class StaticAsteroid : ICloneable
	{
		public ResolvedModel Drawable;
		public Vector3 Rotation;
		public Vector3 Position;
		public Matrix4x4 RotationMatrix;
		public string Info;
        public string Archetype;
        object ICloneable.Clone() => MemberwiseClone();
    }
}

