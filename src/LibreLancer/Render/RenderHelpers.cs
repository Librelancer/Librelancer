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
using LibreLancer.GameData;
namespace LibreLancer
{
	public static class RenderHelpers
	{
		public static float GetZ(Matrix4 world, Vector3 cameraPosition, Vector3 vec)
		{
			var res =  VectorMath.DistanceSquared(world.Transform(vec), cameraPosition);
			return res;
		}
		public static float GetZ(Vector3 cameraPosition, Vector3 vec)
		{
			var res = VectorMath.DistanceSquared(vec, cameraPosition);
			return res;
		}
		public static Lighting ApplyLights(Lighting src, Vector3 c, float r, NebulaRenderer nebula)
		{
			var lights = new Lighting();
			lights.Ambient = src.Ambient;
			foreach (var l in src.Lights)
			{
				if (l.Kind == LightKind.Point &&
					VectorMath.Distance(l.Position, c) > r + l.Range)
					continue;
				lights.Lights.Add(l);
			}
			if (nebula != null)
			{
				Color4? ambient;
				bool fogenabled;
				Vector2 fogrange;
				Color4 fogcolor;
				RenderLight? lightning;
				nebula.GetLighting(out fogenabled, out ambient, out fogrange, out fogcolor, out lightning);
				if (ambient != null)
					lights.Ambient = ambient.Value;
				if (fogenabled)
				{
					lights.FogEnabled = true;
					lights.FogColor = fogcolor;
					lights.FogRange = fogrange;
				}
				if (lightning != null)
				{
					lights.Lights.Add(lightning.Value);
				}
			}
			return lights;
		}
	}
}

