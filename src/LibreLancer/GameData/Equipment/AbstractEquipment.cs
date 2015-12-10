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
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;

using LibreLancer.Ini;

namespace LibreLancer.GameData.Equipment
{
	public abstract class AbstractEquipment
	{
		private Section section;

		public string Nickname { get; private set; }

		protected AbstractEquipment(Section section)
		{
			if (section == null) throw new ArgumentNullException("section");

			this.section = section;
		}

		protected bool parentEntry(Entry e)
		{
			switch (e.Name.ToLowerInvariant())
			{
			case "nickname":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				Nickname = e[0].ToString();
				break;
			default: return false;
			}

			return true;
		}
	}
}
