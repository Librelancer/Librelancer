// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Thn
{
	[Flags]
	public enum AttachFlags
	{
		Position = 2,
		Orientation = 4,
		EntityRelative = 32,
		LookAt = 8,
		OrientationRelative = 128, // Check?
		ParentChild = 64 // Unknown
	}
}

