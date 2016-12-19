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
	public struct Color3f
	{
		public static readonly Color3f White = new Color3f(1, 1, 1);
		public static readonly Color3f Black = new Color3f(0, 0, 0);
		public float R;
		public float G;
		public float B;
		public Color3f(float r, float g, float b)
		{
			R = r;
			G = g;
			B = b;
		}
		public Color3f(Vector3 val) : this(val.X, val.Y, val.Z) {}

		public override string ToString ()
		{
			return string.Format ("[R:{0}, G:{1}, B:{2}]", R, G, B);
		}
	}
}

