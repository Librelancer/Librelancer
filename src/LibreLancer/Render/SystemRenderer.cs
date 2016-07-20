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
using LibreLancer.GameData;
using LibreLancer.GameData.Archetypes;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;
namespace LibreLancer
{
	public class SystemRenderer
	{
		ICamera camera;
		LegacyGameData data;

		public Matrix4 World { get; private set; }

		public List<SunRenderer> Suns { get; private set; }
		public List<ModelRenderer> Models { get; private set; }
		public List<AsteroidFieldRenderer> AsteroidFields { get; private set; }
		public List<NebulaRenderer> Nebulae { get; private set; }

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
		FreelancerGame game;
		Texture2D dot;
		public SystemRenderer(ICamera camera, LegacyGameData data, ResourceManager rescache)
		{
			this.camera = camera;
			this.data = data;
			World = Matrix4.Identity;
			Suns = new List<SunRenderer>();
			Models = new List<ModelRenderer>();
			AsteroidFields = new List<AsteroidFieldRenderer>();
			cache = rescache;
			rstate = cache.Game.RenderState;
			game = rescache.Game;
			dot = new Texture2D(1, 1, false, SurfaceFormat.Color);
			dot.SetData(new uint[] { 0xFFFFFFFF });
		}

		void LoadSystem(StarSystem system)
		{
			starSystem = system;

			Suns.Clear();

			Models.Clear();

			if (starSphereModels != null)
			{
				starSphereModels = new CmpFile[0];
			}

			GC.Collect();
			cache.ClearTextures();

			//Load new system
			starSystem = system;

			List<IDrawable> starSphereRenderData = new List<IDrawable>();
			if (system.StarsBasic != null)
				starSphereRenderData.Add(system.StarsBasic);

			if (system.StarsComplex != null)
				starSphereRenderData.Add(system.StarsComplex);

			if (system.StarsNebula != null)
				starSphereRenderData.Add(system.StarsNebula);

			starSphereModels = starSphereRenderData.ToArray();

			AsteroidFields = new List<AsteroidFieldRenderer>();
			if (system.AsteroidFields != null)
			{
				foreach (var a in system.AsteroidFields)
				{
					AsteroidFields.Add(new AsteroidFieldRenderer(a));
				}
			}

			Nebulae = new List<NebulaRenderer>();
			if (system.Nebulae != null)
			{
				foreach (var n in system.Nebulae)
				{
					Nebulae.Add(new NebulaRenderer(n, camera, cache.Game));
				}
			}

			foreach (SystemObject o in system.Objects)
			{
				if (o.Archetype is Sun)
				{
					SunRenderer s = new SunRenderer(o, game);
					Suns.Add(s);
				}
				else if (o.Archetype.ArchetypeName != "JumpHole")
				{
					ModelRenderer m = new ModelRenderer(camera, World, o, cache, ObjectInNebula(o.Position));
					Models.Add(m);
				}
			}
		
			systemLighting = new Lighting();
			systemLighting.Ambient = system.AmbientColor;
			systemLighting.Lights = system.LightSources;
		}

		public void Update(TimeSpan elapsed)
		{
			foreach (var model in starSphereModels)
				model.Update(camera, elapsed);

			for (int i = 0; i < Suns.Count; i++) Suns[i].Update(elapsed, camera);
			for (int i = 0; i < Models.Count; i++) Models[i].Update(elapsed);
			for (int i = 0; i < AsteroidFields.Count; i++) AsteroidFields[i].Update(camera);
			for (int i = 0; i < Nebulae.Count; i++) Nebulae[i].Update(elapsed);
		}
		NebulaRenderer ObjectInNebula(Vector3 position)
		{
			for (int i = 0; i < Nebulae.Count; i++)
			{
				var n = Nebulae[i];
				if (n.Nebula.Zone.Shape.ContainsPoint(
					n.Nebula.Zone.Position,
					position))
					return n;
			}
			return null;
		}
		NebulaRenderer CheckNebulae()
		{
			for (int i = 0; i < Nebulae.Count; i++)
			{
				var n = Nebulae[i];
				if (n.Nebula.Zone.Shape.ContainsPoint(
					n.Nebula.Zone.Position,
					camera.Position
				))
					return n;
			}
			return null;
		}
		CommandBuffer commands = new CommandBuffer();
		public void Draw()
		{
			NebulaRenderer nr = CheckNebulae(); //are we in a nebula?
			bool transitioned = false;
			if (nr != null)
				transitioned = nr.FogTransitioned();
			rstate.DepthEnabled = true;
			if (transitioned)
			{
				//Fully in fog. Skip Starsphere
				rstate.ClearColor = nr.Nebula.FogColor;
				rstate.ClearAll();
			}
			else
			{
				rstate.DepthEnabled = false;
				rstate.ClearColor = starSystem.BackgroundColor;
				rstate.ClearAll();
				//Starsphere
				for (int i = 0; i < starSphereModels.Length; i++)
				{
					starSphereModels[i].Draw(rstate, Matrix4.CreateTranslation(camera.Position), Lighting.Empty);
				}
				//Render fog transition: if any
				if (nr != null)
				{
					rstate.DepthEnabled = false;
					nr.RenderFogTransition();
					rstate.DepthEnabled = true;
				}
			}
			commands.StartFrame();
			rstate.DepthEnabled = true;
			//Clear depth buffer for game objects
			game.Billboards.Begin(camera, commands);
			for (int i = 0; i < Models.Count; i++) Models[i].Draw(commands, systemLighting, nr);
			for (int i = 0; i < AsteroidFields.Count; i++) AsteroidFields[i].Draw(cache, systemLighting, commands, nr);
			game.Nebulae.NewFrame();
			if (nr == null)
			{
				for (int i = 0; i < Nebulae.Count; i++) Nebulae[i].Draw(commands, systemLighting);
			}
			else
				nr.Draw(commands, systemLighting);
			game.Nebulae.SetData();
			for (int i = 0; i < Suns.Count; i++) Suns[i].Draw(nr);
			game.Billboards.End();
			//Opaque Pass
			rstate.DepthEnabled = true;
			commands.DrawOpaque(rstate);
			//Transparent Pass
			rstate.DepthWrite = false;
			commands.DrawTransparent(rstate);
			rstate.DepthWrite = true;
		}

	}
}

