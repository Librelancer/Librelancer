using System;
using System.IO;
namespace LibreLancer.ImageLib
{
	static class BinaryReaderExtensions
	{
		public static void Skip(this BinaryReader reader, int bytes)
		{
			reader.BaseStream.Seek (bytes, SeekOrigin.Current);
		}
			
		public static int ReadInt32BE(this BinaryReader reader)
		{
			var bytes = reader.ReadBytes (4);
			if (BitConverter.IsLittleEndian) {
				int x = (bytes [0] << 24) | (bytes [1] << 16) | (bytes [2] << 8) | bytes [3];
				return x;
			} else {
				return BitConverter.ToInt32 (bytes, 0);
			}
		}
	}
}

