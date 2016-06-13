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
using System.Linq;

namespace LibreLancer.Utf.Ale
{
	public class AlchemyCurve
	{
		public float SParam;
		public float Value;
		public LoopFlags Flags;
		public List<CurveKeyframe> Keyframes;
		Dictionary<Vector2, BezierFunction> beziers =new Dictionary<Vector2, BezierFunction>();
		float duration = float.NegativeInfinity;
		public float GetValue(float time) {
			if (Keyframes == null)
				return Value;
			if (Keyframes.Count == 1)
				return Keyframes [0].Value;
			throw new NotImplementedException ();
			if (beziers == null)
				GenerateBezierFunctions ();
			if (duration == float.NegativeInfinity)
				CalculateDuration ();
		}
			
		void CalculateDuration()
		{

		}

		void GenerateBezierFunctions()
		{
			//Generate forward functions
			foreach (var pair in Keyframes.Skip(1).Zip(Keyframes, (second, first) => new[] { first, second })) {
				GenerateFunction (pair [0], pair [1]);
			}
			var kf2 = new List<CurveKeyframe> (Keyframes);
			kf2.Reverse ();
			//Generate backwards functions
			foreach (var pair in kf2.Skip(1).Zip(Keyframes, (second, first) => new[] { first, second })) {
				GenerateFunction (pair [0], pair [1]);
			}
			//Connect end to start
			GenerateFunction(Keyframes[Keyframes.Count - 1], Keyframes[0]);
			//Connect start to end
			GenerateFunction (Keyframes[0], Keyframes[Keyframes.Count - 1]);
		}

		void GenerateFunction(CurveKeyframe a, CurveKeyframe b)
		{
			var key = new Vector2 (a.FrameIndex, b.FrameIndex);
			if (beziers.ContainsKey (key))
				return;
			float delta = b.Value - a.Value;
			float p1y = a.OutTangent / delta;
			float p2y = b.InTangent / delta;
			beziers.Add (key, CubicBezier.Bezier (0, p1y, 1, p2y));
		}
	}
}

