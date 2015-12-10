using System;
using LibreLancer.GameData.Universe;
using LibreLancer.GameData.Solar;
using LibreLancer.Utf.Mat;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
	public class PlanetRenderer : ObjectRenderer
	{
		public Planet Planet { get; private set; }
		SphFile sphere;
		public PlanetRenderer (Camera camera, Matrix4 world, bool useObjectPosAndRotate, SystemObject spaceObject)
			: base(camera, world, useObjectPosAndRotate, spaceObject)
		{
			Planet = spaceObject.Archetype as Planet;
			sphere = SpaceObject.Archetype.DaArchetype as SphFile;
			SpaceObject.Initialize ();
			Console.WriteLine (spaceObject.Nickname);
			if (spaceObject.Nickname == "Ku01_01") {
				Console.WriteLine ();
			}
		}
		public override void Update (TimeSpan elapsed)
		{
			sphere.Update (camera);

		}
		public override void Draw (Lighting lights)
		{
			GL.Disable (EnableCap.Blend);
			sphere.Draw (World, lights);
		}
		public override void Dispose ()
		{
			
		}
	}
}

