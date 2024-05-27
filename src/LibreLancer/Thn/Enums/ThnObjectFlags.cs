// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Thn
{
	[Flags]
	public enum ThnObjectFlags
	{
		None = 0,
		LitDynamic = 2,
        SoundSpatial = (1 << 20), // Store differently for reversing
		LitAmbient = 4,
		Hidden = 16,
		Reference = 1
    }
}

