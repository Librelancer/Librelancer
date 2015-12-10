using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using LibreLancer.GameData;
using LibreLancer.GameData.Universe;
using LibreLancer.GameData.Solar;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class SystemRenderer
	{
		Camera camera;
		FreelancerData data;

		public Matrix4 World { get; private set; }

		public List<SunRenderer> Suns { get; private set; }
		public List<ModelRenderer> Models { get; private set; }
		public List<PlanetRenderer> Planets { get; private set; }
		private StarSystem starSystem;
		public StarSystem StarSystem
		{
			get { return starSystem; }
			set { LoadSystem(value); }
		}

		private ModelFile[] starSphereModels;
		Lighting systemLighting;
		public SystemRenderer (Camera camera, FreelancerData data)
		{
			this.camera = camera;
			this.data = data;
			World = Matrix4.Identity;
			Suns = new List<SunRenderer> ();
			Models = new List<ModelRenderer> ();
			Planets = new List<PlanetRenderer> ();
		}

		void LoadSystem(StarSystem system)
		{
			starSystem = system;

			foreach (SunRenderer s in Suns) s.Dispose();
			Suns.Clear();

			foreach (ModelRenderer r in Models) r.Dispose();
			Models.Clear();

			foreach (PlanetRenderer r in Planets) r.Dispose();
			Models.Clear();

			if (starSphereModels != null)
			{
				starSphereModels = new ModelFile[0];
			}

			GC.Collect();

			//Load new system
			starSystem = system;

			List<ModelFile> starSphereRenderData = new List<ModelFile>();

			CmpFile basicStars = system.BackgroundBasicStars;
			if (basicStars != null) starSphereRenderData.AddRange(basicStars.Models.Values);

			IDrawable complexStars = system.BackgroundComplexStars;
			if (complexStars != null)
			{
				if (complexStars is CmpFile)
				{
					CmpFile cmp = complexStars as CmpFile;
					starSphereRenderData.AddRange(cmp.Models.Values);
				}
				else if (complexStars is ModelFile)
				{
					ModelFile model = complexStars as ModelFile;
					starSphereRenderData.Add(model);
				}
			}

			CmpFile nebulae = system.BackgroundNebulae;
			if (nebulae != null)
			{
				starSphereRenderData.AddRange(nebulae.Models.Values);
			}

			starSphereModels = starSphereRenderData.ToArray();

			foreach (ModelFile model in starSphereModels)
				model.Initialize ();

			foreach (SystemObject o in system.Objects)
			{
				if (o.Archetype is Sun) {
					Sun sun = o.Archetype as Sun;
					SunRenderer s = new SunRenderer (camera, World, true, o);
					Suns.Add (s);
				} else if (o.Archetype is Planet) {
					Planet planet = o.Archetype as Planet;
					if (planet.DaArchetype is SphFile) {
						//PlanetRenderer p = new PlanetRenderer(graphicsDevice, content, camera, World, true, o);
						//PlanetRenderer p = new PlanetRenderer (starchart, World, o);
						var p = new PlanetRenderer(camera, World, true, o);
						Console.WriteLine ("sphere planet");
						Planets.Add(p);
					} else {
						Console.WriteLine ("model planet");
						ModelRenderer m = new ModelRenderer (camera, World, true, o);
						Models.Add (m);
					}
				} else if (o.Archetype is TradelaneRing) {
					ModelRenderer m = new ModelRenderer (camera, World, true, o);
					Models.Add (m);
				} else if (o.Archetype is JumpGate) {
					ModelRenderer m = new ModelRenderer (camera, World, true, o);
					Models.Add (m);
				} else if (o.Archetype is JumpHole) {
					//ModelRenderer m = new ModelRenderer (camera, World, true, o);
					//Models.Add (m);
				} else if (o.Archetype is NonTargetable) {
					//Do stuff here
					ModelRenderer m = new ModelRenderer (camera, World, true, o);
					Models.Add (m);
				}
				else
				{
					ModelRenderer m = new ModelRenderer(camera, World, true, o);
					Models.Add(m);
				}
			}
			systemLighting = new Lighting ();
			systemLighting.Ambient = system.AmbientColor ?? Color4.White;

			foreach (var src in system.LightSources) {
				var lt = new RenderLight ();
				lt.Attenuation = src.Attenuation ?? new Vector3 (1, 0, 0);
				lt.Color = src.Color.Value;
				lt.Position = src.Pos.Value;
				lt.Range = src.Range.Value;
				lt.Rotation = src.Rotate ?? Vector3.Zero;
				systemLighting.Lights.Add (lt);
			}
		}

		public void Update(TimeSpan elapsed)
		{
			foreach (ModelFile model in starSphereModels)
				model.Update (camera);

			for (int i = 0; i < Suns.Count; i++) Suns[i].Update(elapsed);
			for (int i = 0; i < Planets.Count; i++) Planets[i].Update(elapsed);
			for (int i = 0; i < Models.Count; i++) Models[i].Update(elapsed);
		}

		public void Draw()
		{
			//StarSphere
			for (int i = 0; i < starSphereModels.Length; i++)
			{
				starSphereModels [i].Draw (Matrix4.CreateTranslation (camera.Position), new Lighting ());
			}
			//starSystem.BackgroundNebulae.Draw (Matrix4.CreateTranslation (camera.Position), new Lighting ());
			GL.Clear (ClearBufferMask.DepthBufferBit);

			//Console.WriteLine ();
			for (int i = 0; i < Suns.Count; i++) Suns[i].Draw(systemLighting);
			for (int i = 0; i < Models.Count; i++) Models[i].Draw(systemLighting);
			for (int i = 0; i < Planets.Count; i++) Planets [i].Draw (systemLighting);
		}
	}
}

