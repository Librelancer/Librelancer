using System;

namespace LibreLancer.Platforms
{
	class LinuxPlatform : IPlatform
	{
		public bool IsDirCaseSensitive (string directory)
		{
			return true;
		}
	}
}

