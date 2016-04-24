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

namespace LibreLancer.Utf.Ale
{
	public class AlchemyFloats
	{
		public float SParam;
		public EasingTypes Type;
		public Tuple<float,float>[] Data;
		public AlchemyFloats ()
		{
		}
		public float GetValue(float time) {
			//Only have one keyframe? Just return it.
			if (Data.Length == 1) {
				return Data [0].Item2;
			}
			//Locate the keyframes to interpolate between
			float t1 = float.NegativeInfinity;
			float t2 = 0, v1 = 0, v2 = 0;
			for (int i = 0; i < Data.Length - 1; i++) {
				if (time >= Data [i].Item1 && time <= Data [i + 1].Item1) {
					t1 = Data [i].Item1;
					t2 = Data [i + 1].Item1;
					v1 = Data [i].Item2;
					v2 = Data [i + 1].Item2;
				}
			}
			//Time wasn't between any values. Return max.
			if (t1 == float.NegativeInfinity) {
				return Data [Data.Length - 1].Item2;
			}
			//Interpolate!
			return AlchemyEasing.Ease(Type,time, t1, t2, v1, v2);
		}
	}
}

