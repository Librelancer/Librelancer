// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.GameData;

namespace LibreLancer.Render
{
	public static class RenderHelpers
	{
        const int MAX_LIGHTS = 8;
		public static float GetZ(Matrix4x4 world, Vector3 cameraPosition, Vector3 vec)
		{
			var res =  Vector3.DistanceSquared(Vector3.Transform(vec,world), cameraPosition);
			return res;
		}
		public static float GetZ(Vector3 cameraPosition, Vector3 vec)
		{
			var res = Vector3.DistanceSquared(vec, cameraPosition);
			return res;
		}
		public static Lighting ApplyLights(SystemLighting src, int lightGroup, Vector3 c, float r, NebulaRenderer nebula, bool lambient = true, bool ldynamic = true, bool nofog = false)
		{
            if (!ldynamic && !lambient)
                return Lighting.Empty;
            ldynamic = true;
            var lights = Lighting.Create();
            lights.Ambient = lambient ? new Color3f(src.Ambient.R, src.Ambient.G, src.Ambient.B) : Color3f.Black;
            for (int i = 0; i < src.Lights.Count; i++)
            {
                if (src.Lights[i].LightGroup != lightGroup)
                    continue;
                if (!src.Lights[i].Active)
                    continue;
                lights.Ambient.R += src.Lights[i].Light.Ambient.R;
                lights.Ambient.G += src.Lights[i].Light.Ambient.G;
                lights.Ambient.B += src.Lights[i].Light.Ambient.B;
            }
            lights.NumberOfTilesX = src.NumberOfTilesX;
			if (nofog)
			{
				lights.FogMode = FogModes.None;
			}
			else
			{
				lights.FogMode = src.FogMode;
                lights.FogColor = new Color3f(src.FogColor.R, src.FogColor.G, src.FogColor.B);
                if (src.FogMode == FogModes.Linear)
                    lights.FogRange = src.FogRange;
                else
                    lights.FogRange = new Vector2(src.FogDensity, 0);
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
					if (l.Kind > 0 && Vector3.DistanceSquared(l.Position, c) > (r2 * r2))
						continue;
					//Advanced spotlight cull
					if ((l.Kind == LightKind.Spotlight) && SpotlightTest(ref l, c, r))
						continue;
                    //if ((lc + 1) > MAX_LIGHTS) throw new Exception("Too many lights!");
                    if ((lc + 1) > MAX_LIGHTS) break;
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
                    lights.Ambient = new Color3f(ambient.Value.R, ambient.Value.G, ambient.Value.B);
				if (fogenabled)
				{
					lights.FogMode = FogModes.Linear;
                    lights.FogColor = new Color3f(fogcolor.R, fogcolor.G, fogcolor.B);
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
			var VLenSq = V.LengthSquared();
			var V1len = Vector3.Dot(V, light.Direction);
			var distClosestPoint = (float)(Math.Cos(light.Phi) * Math.Sqrt(VLenSq - V1len * V1len) - V1len * Math.Sin(light.Phi));
			var angleCull = distClosestPoint > objRadius;
			var frontCull = V1len > objRadius + light.Range;
			var backCull = V1len < -objRadius;
			return angleCull || frontCull || backCull;
		}
	}
}

