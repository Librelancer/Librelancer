// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Utf.Ale
{
	[Flags]
	public enum LoopFlags : ushort
	{
		PlayOnce = 0,
		Repeat = 16,
		Reverse = 32,
		Continue = 64,
		ContinueRepeat = Repeat | Continue
	}
}

