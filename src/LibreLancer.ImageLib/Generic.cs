using System;
using System.IO;
namespace LibreLancer.ImageLib
{
	public static class Generic
	{
		public static Texture2D FromFile(string file)
		{
			using(var stream = File.OpenRead(file)) {
				return FromStream (stream);
			}
		}
		public static Texture2D FromStream(Stream stream)
		{
			if (DDS.StreamIsDDS (stream)) {
				return DDS.DDSFromStream2D (stream, 0, true);
			} else if (PNG.StreamIsPng (stream)) {
				return PNG.FromStream (stream);
			} else if (BMP.StreamIsBMP (stream)) {
				return BMP.FromStream (stream);
			} else {
				return TGA.FromStream (stream);
			}
		}
	}
}

