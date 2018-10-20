// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.GameData
{
	public class ExclusionZone
	{
		public Zone Zone;
		//Shell
		public IDrawable Shell;
		public Color3f ShellTint;
		public float ShellMaxAlpha;
		public float ShellScalar;
		//Fog
		public float FogFar;
	}
}

