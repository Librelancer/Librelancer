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

		public Matrix4 World { get; private set; }

		public List<ObjectRenderer> Objects { get; private set; }
		public List<AsteroidFieldRenderer> AsteroidFields { get; private set; }
		public List<NebulaRenderer> Nebulae { get; private set; }

		private StarSystem starSystem;
		public StarSystem StarSystem
		{
			get { return starSystem; }
			set { LoadSystem(value); }
		}
		
		//Global Renderer Options
		public bool MSAAEnabled = false;
		public float LODMultiplier = 32;
		
		public IDrawable[] StarSphereModels;
		public Matrix4[] StarSphereWorlds;

		public PhysicsDebugRenderer DebugRenderer;
		public SystemLighting SystemLighting = new SystemLighting();
		ResourceManager cache;
		RenderState rstate;
		FreelancerGame game;
		Texture2D dot;

		public FreelancerGame Game
		{
			get
			{
				return game;
			}
		}

		public SystemRenderer(ICamera camera, LegacyGameData data, ResourceManager rescache)
		{
			this.camera = camera;			
			World = Matrix4.Identity;
			Objects = new List<ObjectRenderer>();
			AsteroidFields = new List<AsteroidFieldRenderer>();
			Nebulae = new List<NebulaRenderer>();
			StarSphereModels = new IDrawable[0];
			cache = rescache;
			rstate = cache.Game.RenderState;
			game = rescache.Game;
			dot = new Texture2D(1, 1, false, SurfaceFormat.Color);
			dot.SetData(new uint[] { 0xFFFFFFFF });
			DebugRenderer = new PhysicsDebugRenderer();
		}

		void LoadSystem(StarSystem system)
		{
			starSystem = system;

			if (StarSphereModels != null)
			{
				StarSphereModels = new CmpFile[0];
			}

			if (AsteroidFields != null)
				foreach (var f in AsteroidFields) f.Dispose();
			
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

			StarSphereModels = starSphereRenderData.ToArray();

			AsteroidFields = new List<AsteroidFieldRenderer>();
			if (system.AsteroidFields != null)
			{
				foreach (var a in system.AsteroidFields)
				{
					AsteroidFields.Add(new AsteroidFieldRenderer(a, this));
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
		
			SystemLighting = new SystemLighting();
			SystemLighting.Ambient = system.AmbientColor;
            foreach (var lt in system.LightSources)
				SystemLighting.Lights.Add(new DynamicLight() { Light = lt });
		}
		public void Update(TimeSpan elapsed)
		{
			foreach (var model in StarSphereModels)
				model.Update(camera, elapsed, TimeSpan.FromSeconds(game.TotalTime));

			for (int i = 0; i < AsteroidFields.Count; i++) AsteroidFields[i].Update(camera);
			for (int i = 0; i < Nebulae.Count; i++) Nebulae[i].Update(elapsed);
		}

		public NebulaRenderer ObjectInNebula(Vector3 position)
		{
			for (int i = 0; i < Nebulae.Count; i++)
			{
				var n = Nebulae[i];
				if (n.Nebula.Zone.Shape.ContainsPoint(position))
					return n;
			}
			return null;
		}

		NebulaRenderer CheckNebulae()
		{
			for (int i = 0; i < Nebulae.Count; i++)
			{
				var n = Nebulae[i];
				if (n.Nebula.Zone.Shape.ContainsPoint(camera.Position))
					return n;
			}
			return null;
		}
		MultisampleTarget msaa;
		int _mwidth = -1, _mheight = -1;
		CommandBuffer commands = new CommandBuffer();
		public void Draw()
		{
			if (MSAAEnabled)
			{
				if (_mwidth != Game.Width || _mheight != Game.Height)
				{
					_mwidth = Game.Width;
					_mheight = Game.Height;
					if (msaa != null)
						msaa.Dispose();
					msaa = new MultisampleTarget(Game.Width, Game.Height, 4);
				}
				msaa.Bind();
			}
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
				if (starSystem == null)
					rstate.ClearColor = Color4.Black;
				else
					rstate.ClearColor = starSystem.BackgroundColor;
				rstate.ClearAll();
				//Starsphere
				for (int i = 0; i < StarSphereModels.Length; i++)
				{
					Matrix4 ssworld = Matrix4.CreateTranslation(camera.Position);
					if (StarSphereWorlds != null) ssworld = StarSphereWorlds[i] * ssworld;
					StarSphereModels[i].Draw(rstate, ssworld, Lighting.Empty);
				}
				//Render fog transition: if any
				if (nr != null)
				{
					rstate.DepthEnabled = false;
					nr.RenderFogTransition();
					rstate.DepthEnabled = true;
				}
			}
			DebugRenderer.StartFrame(camera, rstate);
			commands.StartFrame();
			rstate.DepthEnabled = true;
			//Optimisation for dictionary lookups
			LightEquipRenderer.FrameStart();
			//Clear depth buffer for game objects
			game.Billboards.Begin(camera, commands);
			for (int i = 0; i < Objects.Count; i++) Objects[i].Draw(camera, commands, SystemLighting, nr);
			for (int i = 0; i < AsteroidFields.Count; i++) AsteroidFields[i].Draw(cache, SystemLighting, commands, nr);
			game.Nebulae.NewFrame();
			if (nr == null)
			{
				for (int i = 0; i < Nebulae.Count; i++) Nebulae[i].Draw(commands);
			}
			else
				nr.Draw(commands);
			game.Nebulae.SetData();
			game.Billboards.End();
			//Opaque Pass
			rstate.DepthEnabled = true;
			commands.DrawOpaque(rstate);
			//Transparent Pass
			rstate.DepthWrite = false;
			commands.DrawTransparent(rstate);
			rstate.DepthWrite = true;
			DebugRenderer.Render();
			if (MSAAEnabled)
			{
				msaa.BlitToScreen();
			}
		}

	}
}

