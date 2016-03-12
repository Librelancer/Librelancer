using System;

namespace LibreLancer.Utf.Mat
{
	[Flags()]
	public enum SamplerFlags
	{
		None = 0,
		RepeatU = 0x1,
		ClampToEdgeU = 0x2,
		NoRepeatU = 0x3,
		MirrorRepeatV = 0x4,
		MirrorClampToEdgeV = 0x8,
		NoRepeatV = 0xC
	}
}

