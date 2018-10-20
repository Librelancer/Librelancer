// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	[Flags]
	public enum KeyModifiers : ushort
	{
		None = 0x0000,
		LeftShift = 0x0001,
		RightShift = 0x0002,
		LeftControl = 0x0040,
		RightControl = 0x0080,
		LeftAlt = 0x0100,
		RightAlt = 0x0200,
		LeftGUI = 0x0400,
		RightGUI = 0x0800,
		Numlock = 0x1000,
		Capslock = 0x2000,
		Mode = 0x4000,
		Reserved = 0x8000,

		/* These are defines in the SDL headers */
		Control = (LeftControl | RightControl),
		Shift = (LeftShift | RightShift),
		Alt = (LeftAlt | RightAlt),
		GUI = (LeftGUI | RightGUI)
	}
}

