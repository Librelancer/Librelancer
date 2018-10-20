// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.Fx
{
	//Map D3DBLEND pairs to internal blend modes
	public static class BlendMap
	{
		static Dictionary<Tuple<uint, uint>, BlendMode> _map = new Dictionary<Tuple<uint, uint>, BlendMode>();
		static void Add(uint src, uint dest, BlendMode mode)
		{
			_map.Add(new Tuple<uint, uint>(src, dest), mode);
		}
		public static BlendMode Map(Tuple<uint,uint> ale)
		{
			if (ale.Item1 < 1 || ale.Item1 > 17)
				throw new ArgumentException("Ale Blend_Info source out of range");
			if (ale.Item2 < 1 || ale.Item2 > 17)
				throw new ArgumentException("Ale Blend_Info destination out of range");
			
			BlendMode mode;
			if (!_map.TryGetValue(ale, out mode))
				throw new NotImplementedException("Ale Blend_Info Not Implemented: " + ale.Item1 + "," + ale.Item2);
			return mode;
		}
		static BlendMap()
		{
			Add(5, 2, BlendMode.Additive);
			Add(5, 6, BlendMode.Normal);
			Add(2, 4, BlendMode.OneInvSrcColor);
		}
	}
}
