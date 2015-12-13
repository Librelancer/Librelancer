using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using LibreLancer.GameData;
using LibreLancer.GameData.Archetypes;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class SystemRenderer
	{
		Camera camera;
		LegacyGameData data;

		public Matrix4 World { get; private set; }

		public List<SunRenderer> Suns { get; private set; }
		public List<ModelRenderer> Models { get; private set; }

		private StarSystem starSystem;
		public StarSystem StarSystem
		{
			get { return starSystem; }
			set { LoadSystem(value); }
		}

		private IDrawable[] starSphereModels;
		Lighting systemLighting;
		ResourceManager cache;
		public SystemRenderer (Camera camera, LegacyGameData data,ResourceManager rescache)
		{
			this.camera = camera;
			this.data = data;
			World = Matrix4.Identity;
			Suns = new List<SunRenderer> ();
			Models = new List<ModelRenderer> ();
			cache = rescache;
		}

		void LoadSystem(StarSystem system)
		{
			starSystem = system;

			foreach (SunRenderer s in Suns) s.Dispose();
			Suns.Clear();

			foreach (ModelRenderer r in Models) r.Dispose();
			Models.Clear();

			if (starSphereModels != null)
			{
				starSphereModels = new CmpFile[0];
			}

			GC.Collect();

			//Load new system
			starSystem = system;

			List<IDrawable> starSphereRenderData = new List<IDrawable>();
			if (system.StarsBasic != null)
				starSphereRenderData.Add (system.StarsBasic);

			if (system.StarsComplex != null)
				starSphereRenderData.Add (system.StarsComplex);

			if (system.StarsNebula != null)
				starSphereRenderData.Add (system.StarsNebula);

			starSphereModels = starSphereRenderData.ToArray();


			foreach (SystemObject o in system.Objects)
			{
				if (o.Archetype is Sun) {
					SunRenderer s = new SunRenderer (camera, World, true, o);
					Suns.Add (s);
				} else if (o.Archetype.ArchetypeName != "JumpHole") {
					ModelRenderer m = new ModelRenderer (camera, World, true, o, cache);
					Models.Add (m);
				}
			}
			/*if (system.Nebulae != null) {
				foreach (var n in system.Nebulae) {
					Nebulae.Add (new NebulaRenderer (n, cache, camera, data));
				}
			}*/
			systemLighting = new Lighting ();
			systemLighting.Ambient = system.AmbientColor;
			systemLighting.Lights = system.LightSources;
		}

		public void Update(TimeSpan elapsed)
		{
			foreach (var model in starSphereModels)
				model.Update (camera);

			for (int i = 0; i < Suns.Count; i++) Suns[i].Update(elapsed);
			for (int i = 0; i < Models.Count; i++) Models[i].Update(elapsed);
		}

		public void Draw()
		{
			GL.ClearColor (starSystem.BackgroundColor);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			//StarSphere
			for (int i = 0; i < starSphereModels.Length; i++)
			{
				starSphereModels [i].Draw (Matrix4.CreateTranslation(camera.Position), new Lighting ());
			}
			//Clear depth buffer for actual objects
			GL.Clear (ClearBufferMask.DepthBufferBit);
			for (int i = 0; i < Models.Count; i++) Models[i].Draw(systemLighting);
			for (int i = 0; i < Suns.Count; i++) Suns[i].Draw(systemLighting);
		}
	}
}

