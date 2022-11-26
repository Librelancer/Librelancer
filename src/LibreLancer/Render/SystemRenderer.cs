// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Fx;
using LibreLancer.GameData;

namespace LibreLancer.Render
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
		public float LODMultiplier = 1.3f;
		public bool ExtraLights = false; //See comments in Draw() before enabling

		public RigidModel[] StarSphereModels;
		public Matrix4x4[] StarSphereWorlds;
        public Lighting[] StarSphereLightings;
		public LineRenderer DebugRenderer;
        public Action PhysicsHook;
		public PolylineRender Polyline;
		public SystemLighting SystemLighting = new SystemLighting();
        public ParticleEffectPool FxPool;
        public BeamsBuffer Beams;
        public StaticBillboards StaticBillboards = new StaticBillboards();
		ResourceManager cache;
		RenderContext rstate;
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

        public SystemRenderer(ICamera camera, GameDataManager data, GameResourceManager rescache, Game game)
        {
            this.camera = camera;
            AsteroidFields = new List<AsteroidFieldRenderer>();
            Nebulae = new List<NebulaRenderer>();
            StarSphereModels = new RigidModel[0];
            Polyline = new PolylineRender(commands);
            FxPool = new ParticleEffectPool(commands);
            cache = rescache;
            rstate = rescache.Game.RenderContext;
            this.game = game;
            gconfig = game.GetService<GameConfig>();
            billboards = game.GetService<Billboards>();
            resman = game.GetService<ResourceManager>();
            nebulae = game.GetService<NebulaVertices>();
            dot = (Texture2D)rescache.FindTexture(ResourceManager.WhiteTextureName);
            DebugRenderer = new LineRenderer();
            Beams = new BeamsBuffer();
            if (GLExtensions.Features430)
            {
                pointLightBuffer = new ShaderStorageBuffer(MAX_POINTS * (16 * sizeof(float)));
                if (pointLightCull == null)
                    pointLightCull = new ComputeShader(Resources.LoadString("LibreLancer.Shaders.lightingcull.glcompute"));
            }
		}

		void LoadSystem(StarSystem system)
		{
			starSystem = system;

			if (StarSphereModels != null)
			{
				StarSphereModels = new RigidModel[0];
			}

			if (AsteroidFields != null)
				foreach (var f in AsteroidFields) f.Dispose();
                
			//Load new system
			starSystem = system;

			List<RigidModel> starSphereRenderData = new List<RigidModel>();
			if (system.StarsBasic != null)
				starSphereRenderData.Add((system.StarsBasic as IRigidModelFile).CreateRigidModel(true));

            if (system.StarsComplex != null)
                starSphereRenderData.Add((system.StarsComplex as IRigidModelFile).CreateRigidModel(true));

            if (system.StarsNebula != null)
                starSphereRenderData.Add((system.StarsNebula as IRigidModelFile).CreateRigidModel(true));

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
					Nebulae.Add(new NebulaRenderer(n, Game, this));
				}
			}

			SystemLighting = new SystemLighting();
			SystemLighting.Ambient = system.AmbientColor;
			foreach (var lt in system.LightSources)
				SystemLighting.Lights.Add(new DynamicLight() { Light = lt });
		}

		public void Update(double elapsed)
        {
            foreach (var model in StarSphereModels)
                model.Update(camera, game.TotalTime, resman);
            FxPool.Update(elapsed);
			for (int i = 0; i < AsteroidFields.Count; i++) AsteroidFields[i].Update(camera);
			for (int i = 0; i < Nebulae.Count; i++) Nebulae[i].Update(elapsed);
            for (int i = tempFx.Count - 1; i >= 0; i--) {
                tempFx[i].Render.Update(elapsed, tempFx[i].Position, Matrix4x4.CreateTranslation(tempFx[i].Position));
                if (tempFx[i].Render.Finished)
                {
                    tempFx.RemoveAt(i);
                }
            }
        }

        private Vector3[] debugPoints = new Vector3[0];
        public void UseDebugPoints(List<Vector3> list)
        {
            this.debugPoints = list.ToArray();
            list.Clear();
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

        class TemporaryFx
        {
            public ParticleEffectRenderer Render;
            public Vector3 Position;
        }

        private List<TemporaryFx> tempFx = new List<TemporaryFx>();
        public void SpawnTempFx(ParticleEffect fx, Vector3 position)
        {
            var ren = new ParticleEffectRenderer(fx);
            ren.SParam = 0;
            ren.Active = true;
            tempFx.Add(new TemporaryFx() { Render = ren, Position = position });
        }

        public bool ZOverride = false; // Stop Thn Camera from changing Z
		public unsafe void Draw()
		{
            if (Game.Width == 0 || Game.Height == 0) 
                //Don't render on Width/Height = 0
                return;
			if (gconfig.Settings.MSAA > 0)
			{
				if (_mwidth != Game.Width || _mheight != Game.Height)
				{
					_mwidth = Game.Width;
					_mheight = Game.Height;
					if (msaa != null)
						msaa.Dispose();
					msaa = new MultisampleTarget(Game.Width, Game.Height, gconfig.Settings.MSAA);
				}
                rstate.RenderTarget = msaa;
			}
            rstate.AnisotropyLevel = gconfig.Settings.Anisotropy;
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
            /*for (int i = 0; i < World.Objects.Count; i += 16)
			{
				JThreads.Instance.AddTask((o) =>
				{
					var offset = (int)o;
					for (int j = 0; j < 16 && ((j + offset) < World.Objects.Count); j++) World.Objects[j + offset].PrepareRender(camera, nr, this);
				}, i);
			}
			JThreads.Instance.BeginExecute();*/
            foreach (var obj in tempFx)
            {
                obj.Render.PrepareRender(camera, nr, this, false);
            }
            for (int i = 0; i < World.Objects.Count; i++)
            {
                World.Objects[i].PrepareRender(camera, nr, this);
            }
			if (transitioned)
			{
				//Fully in fog. Skip Starsphere
				rstate.ClearColor = nr.Nebula.FogColor;
				rstate.ClearAll();
			}
			else
			{
				if (starSystem == null)
					rstate.ClearColor = NullColor;
				else
					rstate.ClearColor = starSystem.BackgroundColor;
				rstate.ClearAll();
            }
			DebugRenderer.StartFrame(camera, rstate);
			Polyline.SetCamera(camera);
			commands.StartFrame(rstate);
			rstate.DepthEnabled = true;
			//Optimisation for dictionary lookups
			LightEquipRenderer.FrameStart();
			//Clear depth buffer for game objects
			billboards.Begin(camera, commands);
			//JThreads.Instance.FinishExecute(); //Make sure visibility calculations are complete						  
			if (GLExtensions.Features430 && ExtraLights)
			{
				//Forward+ heck yeah!
				//(WORKED AROUND) Lights being culled too aggressively - Pittsburgh planet light, intro_planet_chunks
				//Z test doesn't seem to be working (commented out in shader)
                //May need optimisation
				int plc = pointLights.Count;
				using (var h = pointLightBuffer.Map()) {
					var ptr = (PointLight*)h.Handle;
					for (int i = 0; i < pointLights.Count; i++)
					{
						ptr[i] = pointLights[i];
					}
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
				rstate.DepthFunction = DepthFunction.LessEqual;
                rstate.RenderTarget = null;
                if (gconfig.Settings.MSAA > 0) rstate.RenderTarget = msaa;
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
                Matrix4x4.Invert(p, out p);
				pointLightCull.UniformMatrix4fv("viewMatrix", ref v);
				pointLightCull.UniformMatrix4fv("invProjection", ref p);
				rstate.SSBOMemoryBarrier(); //I don't think these need to be here - confirm then remove?
				pointLightCull.Dispatch((uint)tilesW, (uint)tilesH, 1);
				rstate.SSBOMemoryBarrier();
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
            Beams.Begin(commands, resman, camera);
			foreach (var obj in objects) obj.Draw(camera, commands, SystemLighting, nr);
            Beams.End();
            FxPool.Draw(camera, Polyline, resman, DebugRenderer);
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
            if (!transitioned)
            {
                //Starsphere
                rstate.DepthRange = new Vector2(1, 1);
                if(camera is ThnCamera thn && !ZOverride) thn.DefaultZ();
                for (int i = 0; i < StarSphereModels.Length; i++)
                {
                    Matrix4x4 ssworld = Matrix4x4.CreateTranslation(camera.Position);
                    if (StarSphereWorlds != null) ssworld = StarSphereWorlds[i] * ssworld;
                    var lighting = Lighting.Empty;
                    if (StarSphereLightings != null) lighting = StarSphereLightings[i];
                    //We frustum cull to save on fill rate for low end devices (pi)
                    var mdl = StarSphereModels[i];
                    for (int j = 0; j < mdl.AllParts.Length; j++)
                    {
                        if (!mdl.AllParts[j].Active || mdl.AllParts[j].Mesh == null) continue;
                        var p = mdl.AllParts[j];
                        var w = p.LocalTransform * ssworld;
                        var bsphere = new BoundingSphere(Vector3.Transform(p.Mesh.Center, w), p.Mesh.Radius);
                        if (camera.Frustum.Intersects(bsphere))
                            p.Mesh.DrawImmediate(0, resman, rstate, w, ref lighting, mdl.MaterialAnims); ;
                    }
                }
                if (camera is ThnCamera thn2 && !ZOverride) thn2.CameraZ();
                if (nr != null)
                {
                    //rstate.DepthEnabled = false;
                    nr.RenderFogTransition();
                    //rstate.DepthEnabled = true;
                }

                rstate.DepthRange = new Vector2(0, 1);
            }
			//Transparent Pass
            rstate.DepthWrite = false;
			commands.DrawTransparent(rstate);
			rstate.DepthWrite = true;
            PhysicsHook?.Invoke();
            foreach (var point in debugPoints)
            {
                var lX = point + new Vector3(5, 0, 0);
                var lmX = point + new Vector3(-5, 0, 0);
                var lY = point + new Vector3(0, -5, 0);
                var lmY = point + new Vector3(0, 5, 0);
                var lZ = point + new Vector3(0, 0, 5);
                var lmZ = point + new Vector3(0, 0, -5);
                DebugRenderer.DrawLine(lX, lmX);
                DebugRenderer.DrawLine(lY, lmY);
                DebugRenderer.DrawLine(lZ, lmZ);
            }
            debugPoints = new Vector3[0];
			DebugRenderer.Render();
			if (gconfig.Settings.MSAA > 0)
			{
				msaa.BlitToScreen();
                rstate.RenderTarget = null;
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
            FxPool.Dispose();
			DebugRenderer.Dispose();
            StaticBillboards.Dispose();
            Beams.Dispose();
        }
	}
}

