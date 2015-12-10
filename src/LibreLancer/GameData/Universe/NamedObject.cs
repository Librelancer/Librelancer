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
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;

using OpenTK;
using LibreLancer.Ini;

namespace LibreLancer.GameData.Universe
{
	public abstract class NamedObject
	{
		private Section section;
		protected FreelancerData GameData;

		public string Nickname { get; private set; }
		public Vector3? Pos { get; private set; }
		public Vector3? Rotate { get; private set; }

		protected NamedObject(Section section, FreelancerData data)
		{
			if (section == null) throw new ArgumentNullException("section");

			this.section = section;
			GameData = data;
		}

		protected NamedObject(Vector3 pos, FreelancerData data)
		{
			Pos = pos;
			GameData = data;
		}

		protected virtual bool parentEntry(Entry e)
		{
			switch (e.Name.ToLowerInvariant())
			{
			case "nickname":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				Nickname = e[0].ToString();
				break;
			case "pos":
				if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (Pos != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				Pos = new Vector3(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
				break;
			case "rotate":
				if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (Rotate != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				Rotate = new Vector3(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
				break;
			default: return false;
			}

			return true;
		}

		public override string ToString()
		{
			return Nickname;
		}
	}
}
