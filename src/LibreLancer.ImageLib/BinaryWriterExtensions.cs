using System;
using System.IO;
namespace LibreLancer.ImageLib
{
	static class BinaryWriterExtensions
	{
		public static void WriteInt32BE(this BinaryWriter writer, int val)
		{
			if (BitConverter.IsLittleEndian)
			{
				var bytes = BitConverter.GetBytes(val);
				for (int i = 3; i >= 0; i--)
					writer.Write(bytes[i]);
			}
			else
				writer.Write(val);
		}
	}
}
