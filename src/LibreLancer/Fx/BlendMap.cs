/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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
