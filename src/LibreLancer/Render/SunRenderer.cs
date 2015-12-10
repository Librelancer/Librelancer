using System;
using LibreLancer.Utf.Mat;
using LibreLancer.Primitives;
using LibreLancer.GameData.Solar;
using LibreLancer.GameData.Universe;
using OpenTK;
namespace LibreLancer
{
	public class SunRenderer : ObjectRenderer
	{
		public Sun Sun { get; private set; }
		SunMaterial material;
		VertexBuffer vertexBuffer;
		ElementBuffer elementBuffer;
		int primitiveCount;

		public SunRenderer (Camera camera, Matrix4 world, bool useObjectPosAndRotate, SystemObject sun)
			:base (camera, world, useObjectPosAndRotate, sun)
		{
			Sun = SpaceObject.Archetype as Sun;
			SphFile s = Sun.DaArchetype as SphFile;

			Ellipsoid sphere = new Ellipsoid(new Vector3(s.Radius), 100, 100);
			vertexBuffer = sphere.VertexBuffer;
			elementBuffer = sphere.ElementBuffer;
			primitiveCount = elementBuffer.IndexCount / 3;
			material = new SunMaterial ();
		}
		public override void Update (TimeSpan elapsed)
		{
			material.ViewProjection = camera.ViewProjection;
		}
		public override void Draw (Lighting lights)
		{
			material.World = World;
			material.Use (null, null);
			vertexBuffer.Draw (PrimitiveTypes.TriangleList, 0, 0, primitiveCount);
		}
		public override void Dispose ()
		{
			vertexBuffer.Dispose ();
			elementBuffer.Dispose ();
		}
	}
}

