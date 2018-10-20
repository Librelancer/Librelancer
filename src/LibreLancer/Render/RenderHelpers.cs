// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.GameData;
namespace LibreLancer
{
	public static class RenderHelpers
	{
        const int MAX_LIGHTS = 8;
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
		public static Lighting ApplyLights(SystemLighting src, int lightGroup, Vector3 c, float r, NebulaRenderer nebula, bool lambient = true, bool ldynamic = true, bool nofog = false)
		{
            var lights = Lighting.Create();
			lights.Ambient = lambient ? src.Ambient : Color4.Black;
			lights.NumberOfTilesX = src.NumberOfTilesX;
			if (nofog)
			{
				lights.FogMode = FogModes.None;
			}
			else
			{
				lights.FogMode = src.FogMode;
				lights.FogDensity = src.FogDensity;
				lights.FogColor = src.FogColor;
				lights.FogRange = src.FogRange;
			}
            int lc = 0;
			if (ldynamic)
			{
                lights.Lights.SourceLighting = src;
				for (int i = 0; i < src.Lights.Count; i++)
				{
					if (src.Lights[i].LightGroup != lightGroup)
						continue;
					if (!src.Lights[i].Active) 
						continue;
					var l = src.Lights[i].Light;
					var r2 = r + l.Range;
					//l.Kind > 0 - test if not directional
					if (l.Kind > 0 && VectorMath.DistanceSquared(l.Position, c) > (r2 * r2))
						continue;
					//Advanced spotlight cull
					if ((l.Kind == LightKind.Spotlight) && SpotlightTest(ref l, c, r))
						continue;
                    if ((lc + 1) > MAX_LIGHTS) throw new Exception("Too many lights!");
                    lc++;
                    lights.Lights.SourceEnabled[i] = true;
				}
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
					lights.FogMode = FogModes.Linear;
					lights.FogColor = fogcolor;
					lights.FogRange = fogrange;
				}
				if (lightning != null && src.NumberOfTilesX == -1)
				{
                    if ((lc + 1) > MAX_LIGHTS) throw new Exception("Too many lights!");
                    lights.Lights.Nebula0 = lightning.Value;
                    lights.Lights.NebulaCount = 1;
				}
			}
			return lights;
		}

		//Returns whether or not a spotlight can be culled
		static bool SpotlightTest(ref RenderLight light, Vector3 objPos, float objRadius)
		{
			var V = objPos - light.Position;
			var VLenSq = V.LengthSquared;
			var V1len = Vector3.Dot(V, light.Direction);
			var distClosestPoint = (float)(Math.Cos(light.Phi) * Math.Sqrt(VLenSq - V1len * V1len) - V1len * Math.Sin(light.Phi));
			var angleCull = distClosestPoint > objRadius;
			var frontCull = V1len > objRadius + light.Range;
			var backCull = V1len < -objRadius;
			return angleCull || frontCull || backCull;
		}
	}
}

