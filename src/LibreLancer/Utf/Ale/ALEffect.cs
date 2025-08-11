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
        public List<AlchemyNodeRef> Fx = new();
        public List<(uint Source, uint Target)> Pairs = new();
		public ALEffect ()
		{
		}
	}
}

