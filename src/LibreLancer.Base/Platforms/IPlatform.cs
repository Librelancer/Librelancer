using System;
using SharpFont;
namespace LibreLancer.Platforms
{
	interface IPlatform
	{
		bool IsDirCaseSensitive(string directory);
		Face LoadSystemFace(Library library, string face);
	}
}

