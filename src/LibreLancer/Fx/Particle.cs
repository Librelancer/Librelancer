// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Fx
{
	public struct Particle
	{
		public bool Active;
		public Vector3 Position;
		public Vector3 Normal;
		public float LifeSpan;
		public float TimeAlive;
        public Quaternion Orientation;
		public NodeReference Appearance;
		public NodeReference Emitter;
	}
}

