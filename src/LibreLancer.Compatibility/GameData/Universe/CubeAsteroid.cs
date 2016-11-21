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
using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Universe
{
	public class CubeAsteroid
	{
		public string Name { get; private set; }
		public Vector3 Rotation { get; private set; }
		public Vector3 Position { get; private set; }
		public string Info { get; private set; }

		public CubeAsteroid (Entry e)
		{
			Name = e[0].ToString();
			Position =  new Vector3(e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle());
			Rotation = new Vector3(e[4].ToSingle(), e[5].ToSingle(), e[6].ToSingle());
			Info = e.Count == 8 ? e[7].ToString() : string.Empty;
		}
	}
}

