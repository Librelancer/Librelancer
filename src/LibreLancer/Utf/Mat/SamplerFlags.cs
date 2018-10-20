// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Utf.Mat
{
	[Flags()]
	public enum SamplerFlags
	{
		None = 0,
		MirrorRepeatU = 0x1,
		ClampToEdgeU = 0x2,
		NoRepeatU = 0x3,
		MirrorRepeatV = 0x4,
		ClampToEdgeV = 0x8,
		NoRepeatV = 0xC,
		SecondUV = 0x10,
		Default = 0x40
	}
}

