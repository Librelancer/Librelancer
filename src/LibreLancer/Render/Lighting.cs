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
using System.Collections.Generic;
namespace LibreLancer
{
	public class Lighting
	{
		public static readonly Lighting Empty = new Lighting() { Enabled = false };
		public bool Enabled = true;
		public Color4 Ambient = Color4.White;
		public List<RenderLight> Lights = new List<RenderLight>();
		public bool FogEnabled = false;
		public Color4 FogColor = Color4.White;
		public Vector2 FogRange = Vector2.Zero;

		bool needsHashCalculation = true;
		int _hash = 0;

		public int Hash
		{
			get
			{
				if (needsHashCalculation)
					CalculateHash();
				return _hash;
			}
		}

		void CalculateHash()
		{
			needsHashCalculation = false;
			if (!Enabled)
			{
				_hash = 0;
				return;
			}
			_hash = 17;
			unchecked
			{
				_hash = _hash * 23 + Ambient.GetHashCode();
				foreach (var lt in Lights)
					_hash = _hash * 23 + lt.GetHashCode();
				if (FogEnabled)
				{
					_hash = _hash * 23 + FogColor.GetHashCode();
					_hash = _hash * 23 + FogRange.GetHashCode();
				}
			}
		}

		public Lighting ()
		{
		}
	}
}

