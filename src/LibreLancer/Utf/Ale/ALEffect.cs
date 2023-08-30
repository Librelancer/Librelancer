// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
namespace LibreLancer.Utf.Ale
{
	public class ALEffect
	{
		public string Name;
		public uint CRC;
		public List<AlchemyNodeRef> FxTree;
		public List<AlchemyNodeRef> Fx;
		public List<(uint Source, uint Target)> Pairs;
		public ALEffect ()
		{
		}
		public AlchemyNodeRef FindRef(uint index)
		{
			var result = from AlchemyNodeRef r in Fx where r.Index == index select r;
			if (result.Count() == 1)
				return result.First();
			throw new Exception();
		}
	}
}

