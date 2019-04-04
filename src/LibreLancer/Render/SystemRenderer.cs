// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.GameData;
using LibreLancer.Utf.Cmp;
namespace LibreLancer
{
	//Responsible for rendering the GameWorld.
	public class SystemRenderer : IDisposable
	{
        public ICamera Camera
        {
            get { return camera;  }
            set { camera = value;  }
        }
        ICamera camera;

		public Color4 NullColor = Color4.Black;

        public GameWorld World { get; set; }
		public List<AsteroidFieldRenderer> AsteroidFields { get; private set; }
		public List<NebulaRenderer> Nebulae { get; private set; }

		private StarSystem starSystem;
		public StarSystem StarSystem
		{
			get { return starSystem; }
			set { LoadSystem(value); }
		}
		
		//Global Renderer Options
		public float LODMultiplier = 2;
		public bool ExtraLights = false; //See comments in Draw() before enabling

		public IDrawable[] StarSphereModels;
		public Matrix4[] StarSphereWorlds;
		public PhysicsDebugRenderer DebugRenderer;
		public PolylineRender Polyline;
		public SystemLighting SystemLighting = new SystemLighting();
		ResourceManager cache;
		RenderState rstate;
		Game game;
		Texture2D dot;

		//Fancy Forward+ stuff (GL 4.3 up)
		List<PointLight> pointLights = new List<PointLight>();
		static ComputeShader pointLightCull;
		ShaderStorageBuffer pointLightBuffer;
		ShaderStorageBuffer transparentLightBuffer;
		//ShaderStorageBuffer opaqueLightBuffer;
		const int MAX_POINTS = 1024;

		public Game Game
		{
			get
			{
				return game;
			}
		}

        public Billboards Billboards
        {
            get
            {
                return billboards;
            }
        }

        GameConfig gconfig;
        Billboards billboards;
        ResourceManager resman;
        NebulaVertices nebulae;

        public ResourceManager ResourceManager
        {
            get
            {
                return resman;
            }
        }

		public SystemRenderer(ICamera camera, GameDataManager data, ResourceManager rescache, Game game)
		{
			this.camera = camera;			
			AsteroidFields = new List<AsteroidFieldRenderer>();
			Nebulae = new List<NebulaRenderer>();
			StarSphereModels = new IDrawable[0];
			Polyline = new PolylineRender(commands);
			cache = rescache;
			rstate = cache.Game.RenderState;
			this.game = game;
            gconfig = game.GetService<GameConfig>();
            billboards = game.GetService<Billboards>();
            resman = game.GetService<ResourceManager>();
            nebulae = game.GetService<NebulaVertices>();
			dot = (Texture2D)rescache.FindTexture(ResourceManager.WhiteTextureName);
			DebugRenderer = new PhysicsDebugRenderer();

			if (GLExtensions.Features430)
			{
				pointLightBuffer = new ShaderStorageBuffer(MAX_POINTS * (16 * sizeof(float)));
				if(pointLightCull == null)
					pointLightCull = new ComputeShader(Resources.LoadString("LibreLancer.Shaders.lightingcull.glcompute"));
			}
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
					Nebulae.Add(new NebulaRenderer(n, camera, Game));
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

		//ExtraLights: Render a point light with DX attenuation
		//TODO: Allow for cubic / IGraph attenuation
		public void PointLightDX(Vector3 position, float range, Color4 color, Vector3 attenuation)
		{
			if (!GLExtensions.Features430 || !ExtraLights) 
				return;
			var lt = new PointLight();
			lt.Position = new Vector4(position, 1);
			lt.ColorRange = new Vector4(color.R, color.G, color.B, range);
			lt.Attenuation = new Vector4(attenuation, 0);
			lock(pointLights) {
				if (pointLights.Count >= 1023)
					return;
				pointLights.Add(lt); //TODO: Alternative to Locking this? Try ConcurrentBag<T> again maybe.
			}
		}

		MultisampleTarget msaa;
		int _mwidth = -1, _mheight = -1;
		CommandBuffer commands = new CommandBuffer();
		int _twidth = -1, _theight = -1;
		int _dwidth = -1, _dheight = -1;
		DepthMap depthMap;

        List<ObjectRenderer> objects;

        public void AddObject(ObjectRenderer render)
        {
            lock (objects)
            {
                objects.Add(render);
            }
        }
		public unsafe void Draw()
		{
			if (gconfig.MSAASamples > 0)
			{
				if (_mwidth != Game.Width || _mheight != Game.Height)
				{
					_mwidth = Game.Width;
					_mheight = Game.Height;
					if (msaa != null)
						msaa.Dispose();
					msaa = new MultisampleTarget(Game.Width, Game.Height, gconfig.MSAASamples);
				}
				msaa.Bind();
			}
			NebulaRenderer nr = CheckNebulae(); //are we in a nebula?

			bool transitioned = false;
			if (nr != null)
				transitioned = nr.FogTransitioned();
			rstate.DepthEnabled = true;
			//Add Nebula light
			if (GLExtensions.Features430 && ExtraLights)
			{
				//TODO: Re-add [LightSource] to the compute shader, it shouldn't regress.
				PointLight p2;
				if (nr != null && nr.DoLightning(out p2))
					pointLights.Add(p2);
			}
            //Async calcs
            objects = new List<ObjectRenderer>(250);
            for (int i = 0; i < World.Objects.Count; i += 16)
			{
				JThreads.Instance.AddTask((o) =>
				{
					var offset = (int)o;
					for (int j = 0; j < 16 && ((j + offset) < World.Objects.Count); j++) World.Objects[j + offset].PrepareRender(camera, nr, this);
				}, i);
			}
			JThreads.Instance.BeginExecute();
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
					rstate.ClearColor = NullColor;
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
			Polyline.SetCamera(camera);
			commands.StartFrame(rstate);
			rstate.DepthEnabled = true;
			//Optimisation for dictionary lookups
			LightEquipRenderer.FrameStart();
			//Clear depth buffer for game objects
			rstate.ClearDepth();
			billboards.Begin(camera, commands);
			JThreads.Instance.FinishExecute(); //Make sure visibility calculations are complete						  
			if (GLExtensions.Features430 && ExtraLights)
			{
				//Forward+ heck yeah!
				//ISSUES: Z prepass here doesn't work - gives blank texture  (investigate DepthMap.cs)
				//(WORKED AROUND) Lights being culled too aggressively - Pittsburgh planet light, intro_planet_chunks
				//Z test - cull transparent and opaque differently (opaqueLightBuffer enable)
				//Optimisation work needs to be done
				//When these are fixed this can be enabled by default
				//Copy lights into buffer
				int plc = pointLights.Count;
				using (var h = pointLightBuffer.Map()) {
					var ptr = (PointLight*)h.Handle;
					for (int i = 0; i < pointLights.Count; i++)
					{
						ptr[i] = pointLights[i];
					}
					//Does the rest of the buffer need to be cleared?
				}
				pointLights.Clear();
				//Setup Visible Buffers
				var tilesW = (Game.Width + (Game.Width % 16)) / 16;
				var tilesH = (Game.Height + (Game.Height % 16)) / 16;
				SystemLighting.NumberOfTilesX = tilesW;
				if (_twidth != tilesW || _theight != tilesH)
				{
					_twidth = tilesW;
					_theight = tilesH;
					//if (opaqueLightBuffer != null) opaqueLightBuffer.Dispose();
					if (transparentLightBuffer != null) transparentLightBuffer.Dispose();
					//opaqueLightBuffer = new ShaderStorageBuffer((tilesW * tilesH) * 512 * sizeof(int)); 
					transparentLightBuffer = new ShaderStorageBuffer((tilesW * tilesH) * 512 * sizeof(int)); 
				}
				//Depth
				if (_dwidth != Game.Width || _dheight != Game.Height)
				{
					_dwidth = Game.Width;
					_dheight = Game.Height;
					if (depthMap != null) depthMap.Dispose();
					depthMap = new DepthMap(Game.Width, game.Height);
				}
				depthMap.BindFramebuffer();
				rstate.ClearDepth();
				rstate.DepthFunction = DepthFunction.Less;
                foreach (var obj in objects) obj.DepthPrepass(camera, rstate);
				//for (int i = 0; i < Objects.Count; i++) if (Objects[i].Visible) Objects[i].DepthPrepass(camera, rstate);
				rstate.DepthFunction = DepthFunction.LessEqual;
				RenderTarget2D.ClearBinding();
				if (gconfig.MSAASamples > 0) msaa.Bind();
				//Run compute shader
				pointLightBuffer.BindIndex(0);
				transparentLightBuffer.BindIndex(1);
				//opaqueLightBuffer.BindIndex(2);
				pointLightCull.Uniform1i("depthTexture", 7);
				depthMap.BindTo(7);
				pointLightCull.Uniform1i("numLights", plc);
				pointLightCull.Uniform1i("windowWidth", Game.Width);
				pointLightCull.Uniform1i("windowHeight", Game.Height);
				var v = camera.View;
				var p = camera.Projection;
				p.Invert();
				pointLightCull.UniformMatrix4fv("viewMatrix", ref v);
				pointLightCull.UniformMatrix4fv("invProjection", ref p);
				GL.MemoryBarrier(GL.GL_SHADER_STORAGE_BARRIER_BIT); //I don't think these need to be here - confirm then remove?
				pointLightCull.Dispatch((uint)tilesW, (uint)tilesH, 1);
				GL.MemoryBarrier(GL.GL_SHADER_STORAGE_BARRIER_BIT);
			}
			else
			{
				SystemLighting.NumberOfTilesX = -1;
                //Simple depth pre-pass
                rstate.ColorWrite = false;
				rstate.DepthFunction = DepthFunction.Less;
                foreach (var obj in objects) obj.DepthPrepass(camera, rstate);
				rstate.DepthFunction = DepthFunction.LessEqual;
                rstate.ColorWrite = true;
			}
			//Actual Drawing
			foreach (var obj in objects) obj.Draw(camera, commands, SystemLighting, nr);
			for (int i = 0; i < AsteroidFields.Count; i++) AsteroidFields[i].Draw(cache, SystemLighting, commands, nr);
			nebulae.NewFrame();
			if (nr == null)
			{
				for (int i = 0; i < Nebulae.Count; i++) Nebulae[i].Draw(commands);
			}
			else
				nr.Draw(commands);
			nebulae.SetData();
			billboards.End();
			Polyline.FrameEnd();
			//Opaque Pass
			rstate.DepthEnabled = true;
			commands.DrawOpaque(rstate);
			//Transparent Pass
			rstate.DepthWrite = false;
			commands.DrawTransparent(rstate);
			rstate.DepthWrite = true;
			DebugRenderer.Render();
			if (gconfig.MSAASamples > 0)
			{
				msaa.BlitToScreen();
			}
			rstate.DepthEnabled = true;
		}

		public void Dispose()
		{
			if (pointLightBuffer != null) pointLightBuffer.Dispose();
			if (transparentLightBuffer != null) transparentLightBuffer.Dispose();
			if (msaa != null) msaa.Dispose();
			if (depthMap != null) depthMap.Dispose();
			Polyline.Dispose();
			DebugRenderer.Dispose();
		}
	}
}

