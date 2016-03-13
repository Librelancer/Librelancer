using System;
using System.Collections.Generic;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyNodeRef
	{
		public uint Flag;
		public uint CRC;
		public uint Parent;
		public uint Index;
		public List<AlchemyNodeRef> Children = new List<AlchemyNodeRef> ();
		public AlchemyNodeRef(uint flg, uint crc, uint parent, uint idx)
		{
			Flag = flg;
			CRC = crc;
			Parent = parent;
			Index = idx;
		}
		public bool IsAttachmentNode {
			get {
				return Flag == 1;
			}
		}
	}
}

