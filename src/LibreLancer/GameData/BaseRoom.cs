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
using System.Collections.Generic;
namespace LibreLancer.GameData
{
	public class BaseRoom
	{
		public string Nickname;
		public string Camera;
		public List<string> ThnPaths;
		public List<BaseHotspot> Hotspots;
		public string Music;

		public IEnumerable<ThnScript> OpenScripts()
		{
			foreach (var p in ThnPaths) yield return new ThnScript(p);
		}
	}
	public class BaseHotspot
	{
		public string Name;
		public string Behaviour;
		public string Room;
		public bool RoomIsVirtual;
	}
}
