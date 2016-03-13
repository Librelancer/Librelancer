using System;
using SharpFont;
namespace LibreLancer.Platforms
{
	class LinuxPlatform : IPlatform
	{
		public bool IsDirCaseSensitive (string directory)
		{
			return true;
		}

		public Face LoadSystemFace (Library library, string face)
		{
			throw new NotImplementedException ();
		}
	}
}

