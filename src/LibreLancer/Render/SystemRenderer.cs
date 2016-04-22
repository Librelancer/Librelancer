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
		ICamera camera;
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
		RenderState rstate;
		public SystemRenderer (ICamera camera, LegacyGameData data,ResourceManager rescache)
		{
			this.camera = camera;
			this.data = data;
			World = Matrix4.Identity;
			Suns = new List<SunRenderer> ();
			Models = new List<ModelRenderer> ();
			cache = rescache;
			rstate = cache.Game.RenderState;
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
				model.Update (camera, elapsed);

			for (int i = 0; i < Suns.Count; i++) Suns[i].Update(elapsed);
			for (int i = 0; i < Models.Count; i++) Models[i].Update(elapsed);
		}

		public void Draw()
		{
			rstate.ClearColor = starSystem.BackgroundColor;
			rstate.ClearAll ();
			rstate.DepthEnabled = true;
			//StarSphere
			for (int i = 0; i < starSphereModels.Length; i++)
			{
				starSphereModels [i].Draw (rstate, Matrix4.CreateTranslation(camera.Position), new Lighting ());
			}
			//Clear depth buffer for game objects
			rstate.ClearDepth();
			for (int i = 0; i < Models.Count; i++) Models[i].Draw(rstate, systemLighting);
			for (int i = 0; i < Suns.Count; i++) Suns[i].Draw(rstate, systemLighting);
		}
	}
}

