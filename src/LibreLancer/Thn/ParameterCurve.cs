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
using LibreLancer.Thorn;
namespace LibreLancer
{
	public enum PCurveType
	{
		Unknown,
		FreeForm,
		BumpIn,
		BumpOut,
		RampDown,
		RampUp,
		Step,
		Smooth,
		ThornL, //?
		Linear,
		CatmullRom
	}
	public class ParameterCurve
	{
		public string CLSID;
		public PCurveType Type = PCurveType.Unknown;
		public List<Vector4> Points;
		public float Period;

		public ParameterCurve() { }
		public ParameterCurve(LuaTable table)
		{
			CLSID = (string)table["CLSID"];
			switch (CLSID.ToLowerInvariant())
			{
				case "freeformpcurve":
					Type = PCurveType.FreeForm;
					break;
				case "bumpinpcurve":
					Type = PCurveType.BumpIn;
					break;
				case "bumpoutpcurve":
					Type = PCurveType.BumpOut;
					break;
				case "rampdownpcurve":
					Type = PCurveType.RampDown;
					break;
				case "rampuppcurve":
					Type = PCurveType.RampUp;
					break;
				case "steppcurve":
					Type = PCurveType.Step;
					break;
				case "smoothpcurve":
					Type = PCurveType.Step;
					break;
				case "thornlpcurve":
					Type = PCurveType.ThornL;
					break;
				case "linearpcurve":
					Type = PCurveType.Linear;
					break;
				case "catmullrompcurve":
					Type = PCurveType.CatmullRom;
					break;
			}
			var points = (LuaTable)table["points"];
			Points = new List<Vector4>();
			for (int i = 0; i < points.Capacity; i++) {
				var p = (LuaTable)points[i];
				var v = new Vector4((float)p[0], (float)p[1], (float)p[2], (float)p[3]);
				Points.Add(v);
			}
		}

		public float GetValue(float time, float duration)
		{
			float x = 0;
			var p = Period / 1000f;
			if (p < 0)
				x = time / duration;
			else
				x = (time % p) / p;
			if (x <= 0)
				return Points[0].Y;
			if (x >= 1)
				return Points[Points.Count - 1].Y;
			//X - time, Y - value, Z - in, W - out
			Vector4 a = Vector4.One;
			Vector4 b = Vector4.One;

			for (int i = 0; i < Points.Count - 1; i++)
			{
				if (x >= Points[i].X && x <= Points[i + 1].X)
				{
					a = Points[i];
					b = Points[i + 1];
				}
			}
			/*switch (Type)
			{
				case PCurveType.FreeForm:
					break;
				default:
					throw new NotImplementedException("PCurveType " + Type.ToString());
			}*/
			return Utf.Ale.AlchemyEasing.Ease(Utf.Ale.EasingTypes.Linear, x, a.X, b.X, a.Y, b.Y);


		}
	}
}
