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
	public class ExclusionZone
	{
		private StarSystem parent;

		private string exclusionName;
		private Zone exclusion;
		public Zone Exclusion
		{
			get
			{
				if (exclusion == null) exclusion = parent.FindZone(exclusionName);
				return exclusion;
			}
		}

		public float? FogFar { get; set; }
		public float? ShellScalar { get; set; }
		public float? MaxAlpha { get; set; }
		public Color4? Color { get; set; }
		public Color3f? Tint { get; set; }
		public int? ExcludeBillboards { get; set; }
		public int? ExcludeDynamicAsteroids { get; set; }
		public float? EmptyCubeFrequency { get; set; }
		public int? BillboardCount { get; set; }
		public string ZoneShellPath { get; set; }

		public ExclusionZone(StarSystem parent, string exclusion)
		{
			this.parent = parent;
			this.exclusionName = exclusion;
		}
	}
}