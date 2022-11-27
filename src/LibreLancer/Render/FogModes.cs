// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Render
{
	public enum FogModes : byte
	{
		None = 0, //NOTE: THIS IS HARDCODED IN THE LIGHTING.INC SHADER
		Linear = 3,
		Exp = 1,
		Exp2 = 2
	}
}

