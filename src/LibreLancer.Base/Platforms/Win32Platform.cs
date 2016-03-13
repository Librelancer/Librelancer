using System;

namespace LibreLancer.Platforms
{
	class Win32Platform : IPlatform
	{
		public bool IsDirCaseSensitive(string directory)
		{
			return false;
		}
	}
}

