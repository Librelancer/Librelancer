// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Interface
{
	public class HudManeuver
	{
		public string Action;
		public int InfocardA;
		public int InfocardB;
		public string ActiveModel;
		public string InactiveModel;

		public HudManeuver(Entry e)
		{
			Action = e[0].ToString();
			InfocardA = e[1].ToInt32();
			InfocardB = e[2].ToInt32();
			ActiveModel = e[3].ToString();
			InactiveModel = e[4].ToString();
		}
	}
}
