using System;
using System.Collections.Generic;
namespace LibreLancer.Utf.Ale
{
	public class ALEffect
	{
		public string Name;
		public List<AlchemyNodeRef> FxTree;
		public List<AlchemyNodeRef> Fx;
		public List<Tuple<uint,uint>> Pairs;
		public ALEffect ()
		{
		}
	}
}

