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
	}
}

