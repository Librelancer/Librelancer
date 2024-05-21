// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Graphics;

namespace LibreLancer.Fx
{
	//Map D3DBLEND pairs to internal blend modes
	public static class BlendMap
	{
		public static ushort Map(Tuple<uint,uint> ale)
		{
			if (ale.Item1 < 1 || ale.Item1 > 11)
				throw new ArgumentException($"Ale Blend_Info source '{ale.Item1}' out of range (1-11)");
			if (ale.Item2 < 1 || ale.Item2 > 11)
				throw new ArgumentException($"Ale Blend_Info destination '{ale.Item2}' out of range (1-11)");
            return BlendMode.Create((ushort)ale.Item1, (ushort)ale.Item2);
        }
	}
}
