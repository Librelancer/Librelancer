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

namespace LibreLancer.Compatibility.GameData.Universe
{
	public class JumpReference
	{
		private UniverseIni universe;

		private string toSystemName;
		private StarSystem toSystem;
		public StarSystem System
		{
			get
			{
				if (toSystem == null) toSystem = universe.FindSystem(toSystemName);
				return toSystem;
			}
		}

		public string Direction { get; private set; }
		public string TunnelEffect { get; private set; }

		public JumpReference(UniverseIni universe, string toSystem, string direction, string tunnelEffect)
		{
			this.universe = universe;
			this.toSystemName = toSystem;
			this.Direction = direction;
			this.TunnelEffect = tunnelEffect;
		}
	}
}