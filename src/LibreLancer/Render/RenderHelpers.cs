using System;
namespace LibreLancer
{
	public static class RenderHelpers
	{
		public static float GetZ(Matrix4 world, Vector3 cameraPosition, Vector3 vec)
		{
			var trans = world.Transform(vec);

			var res =  VectorMath.Distance(world.Transform(vec), cameraPosition);
			return res;
		}
		public static Lighting ApplyLights(Lighting src, Vector3 c, float r)
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
			return lights;
		}
	}
}

