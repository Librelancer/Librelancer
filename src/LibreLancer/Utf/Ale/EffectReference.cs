using System;

namespace LibreLancer.Utf.Ale
{
	public struct EffectReference
	{
		public uint Flag;
		public uint CRC;
		public uint Parent;
		public uint Index;
		public EffectReference(uint flg, uint crc, uint parent, uint idx)
		{
			Flag = flg;
			CRC = crc;
			Parent = parent;
			Index = idx;
		}
	}
}

