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
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using OpenTK;

using LibreLancer.Ini;
using LibreLancer.Utf;

namespace LibreLancer.GameData.Universe
{
	public abstract class SystemPart : NamedObject
	{
		private Section section;

		public string IdsName { get; private set; }
		public List<XmlDocument> IdsInfo { get; private set; }
		public Vector3? Size { get; private set; }
		public Vector3? Spin { get; private set; }
		public string Reputation { get; private set; }
		public int? Visit { get; private set; }

		protected SystemPart(Section section, FreelancerData data)
			: base(section, data)
		{
			if (section == null) throw new ArgumentNullException("section");

			this.section = section;
		}

		protected override bool parentEntry(Entry e)
		{
			if (base.parentEntry(e)) return true;
			else
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "ids_name":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (IdsName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					IdsName = GameData.Infocards.GetStringResource(e[0].ToInt32());
					break;
				case "ids_info":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (IdsInfo == null) IdsInfo = new List<XmlDocument>();
					IdsInfo.Add(GameData.Infocards.GetXmlResource(e[0].ToInt32()));
					break;
				case "size":
					if (e.Count < 1 || e.Count > 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Size != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Vector3 size = new Vector3(e[0].ToSingle());
					if (e.Count > 1) size.Y = e[1].ToSingle();
					if (e.Count > 2) size.Z = e[2].ToSingle();
					Size = size;
					break;
				case "spin":
					if (e.Count > 3)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Spin != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + section.Name);
					float x = 0f, y = 0f, z = 0f;
					if (e.Count > 0)
						x = e [0].ToSingle ();
					if (e.Count > 1)
						y = e [1].ToSingle ();
					if (e.Count > 2)
						z = e [2].ToSingle ();
					Spin = new Vector3(x, y, z);
					break;
				case "reputation":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					//if (Reputation != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Reputation = e[0].ToString(); //TODO: are multiple reputation entries valid?
					break;
				case "visit":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Visit != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Visit = e[0].ToInt32();
					break;
				default: return false;
				}

				return true;
			}
		}
	}
}
