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

namespace LibreLancer
{
	[Flags]
	public enum KeyModifiers : ushort
	{
		None = 0x0000,
		LeftShift = 0x0001,
		RightShift = 0x0002,
		LeftControl = 0x0040,
		RightControl = 0x0080,
		LeftAlt = 0x0100,
		RightAlt = 0x0200,
		LeftGUI = 0x0400,
		RightGUI = 0x0800,
		Numlock = 0x1000,
		Capslock = 0x2000,
		Mode = 0x4000,
		Reserved = 0x8000,

		/* These are defines in the SDL headers */
		Control = (LeftControl | RightControl),
		Shfit = (LeftShift | RightShift),
		Alt = (LeftAlt | RightAlt),
		GUI = (LeftGUI | RightGUI)
	}
}

