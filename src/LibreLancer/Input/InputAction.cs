// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
	public struct InputAction
	{
		public const int ID_THROTTLEUP = 1;
		public const int ID_THROTTLEDOWN = 2;
		public const int ID_STRAFELEFT = 3;
		public const int ID_STRAFERIGHT = 4;
		public const int ID_STRAFEUP = 5;
		public const int ID_STRAFEDOWN = 6;
		public const int ID_THRUST = 7;
		public const int ID_TOGGLECRUISE = 8;
		public const int ID_CANCEL = 9;
		public const int ID_TOGGLEMOUSEFLIGHT = 10;

		public int ID;
		public string Name;
		public Keys Primary;
		public KeyModifiers PrimaryModifiers;
		public Keys Secondary;
		public KeyModifiers SecondaryModifiers;

		public bool IsDown;
		public bool IsToggle;

		public InputAction(int id,
		                   string name,
		                   bool toggle,
		                   Keys primary = Keys.Unknown, 
		                   KeyModifiers primaryMod = KeyModifiers.None,
		                   Keys secondary = Keys.Unknown,
		                   KeyModifiers secondaryMod = KeyModifiers.None
		                  )
		{
			ID = id;
			IsToggle = toggle;
			Name = name;
			Primary = primary;
			PrimaryModifiers = primaryMod;
			Secondary = secondary;
			SecondaryModifiers = secondaryMod;
			IsDown = false;
		}
	}
}
