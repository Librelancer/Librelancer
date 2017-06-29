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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
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
