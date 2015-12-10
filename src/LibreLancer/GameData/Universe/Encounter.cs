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

using System.Collections.Generic;

namespace LibreLancer.GameData.Universe
{
	public class Encounter
	{
		public string EncounterType { get; set; }
		public int Attr2 { get; set; }
		public float Attr3 { get; set; }
		public Dictionary<string, float> Factions { get; set; }

		public Encounter(string attr1, int attr2, float attr3)
		{
			EncounterType = attr1; 
			Attr2 = attr2; 
			Attr3 = attr3;

			Factions = new Dictionary<string, float>();
		}
	}
}