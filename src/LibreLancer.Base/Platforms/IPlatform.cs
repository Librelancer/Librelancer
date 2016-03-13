using System;

namespace LibreLancer.Platforms
{
	interface IPlatform
	{
		bool IsDirCaseSensitive(string directory);
	}
}

