using System;

namespace LibreLancer
{
	[Flags]
	public enum AttachFlags
	{
		Position = 2,
		Orientation = 4,
		EntityRelative = 8,
		LookAt = 16
	}
}

