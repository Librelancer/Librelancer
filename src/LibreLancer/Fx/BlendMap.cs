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
        /* typedef enum D3DBLEND { 
  D3DBLEND_ZERO             = 1,
  D3DBLEND_ONE              = 2,
  D3DBLEND_SRCCOLOR         = 3,
  D3DBLEND_INVSRCCOLOR      = 4,
  D3DBLEND_SRCALPHA         = 5,
  D3DBLEND_INVSRCALPHA      = 6,
  D3DBLEND_DESTALPHA        = 7,
  D3DBLEND_INVDESTALPHA     = 8,
  D3DBLEND_DESTCOLOR        = 9,
  D3DBLEND_INVDESTCOLOR     = 10,
  D3DBLEND_SRCALPHASAT      = 11,
  D3DBLEND_BOTHSRCALPHA     = 12,
  D3DBLEND_BOTHINVSRCALPHA  = 13,
  D3DBLEND_BLENDFACTOR      = 14,
  D3DBLEND_INVBLENDFACTOR   = 15,
  D3DBLEND_SRCCOLOR2        = 16,
  D3DBLEND_INVSRCCOLOR2     = 17,
  D3DBLEND_FORCE_DWORD      = 0x7fffffff
} D3DBLEND, *LPD3DBLEND; */
        static BlendMap()
		{
			Add(5, 2, BlendMode.Additive);
            Add(5, 10, BlendMode.SrcAlphaInvDestColor);
            Add(10, 5, BlendMode.InvDestColorSrcAlpha);
            Add(9, 3, BlendMode.DestColorSrcColor);
			Add(5, 6, BlendMode.Normal);
			Add(2, 4, BlendMode.OneInvSrcColor);
		}
	}
}
