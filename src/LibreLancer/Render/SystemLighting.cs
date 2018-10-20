// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class SystemLighting
	{
		public Color4 Ambient = Color4.Black;
		public List<DynamicLight> Lights = new List<DynamicLight>();
		public FogModes FogMode = FogModes.None;
		public float FogDensity = 0f;
		public Color4 FogColor = Color4.Black;
		public Vector2 FogRange = Vector2.Zero;
		public int NumberOfTilesX;
	}
}
