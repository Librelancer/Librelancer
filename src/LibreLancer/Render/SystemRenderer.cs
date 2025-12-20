// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Fx;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Backends.OpenGL;
using LibreLancer.Render.Materials;
using LibreLancer.Resources;
using LibreLancer.Thn;
using LibreLancer.World;

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
        public Color4? BackgroundOverride;

        public GameWorld World { get; set; }
		public List<AsteroidFieldRenderer> AsteroidFields { get; private set; }
		public List<NebulaRenderer> Nebulae { get; private set; }

		private StarSystem starSystem;
		public StarSystem StarSystem => starSystem;

        //Editor Options
        public bool DrawNebulae = true;
        public bool DrawStarsphere = true;

        //Global Renderer Options
		public bool ExtraLights = false; //See comments in Draw() before enabling

		public RigidModel[] StarSphereModels;
		public Matrix4x4[] StarSphereWorlds;
        public Lighting[] StarSphereLightings;
		public LineRenderer DebugRenderer;
        public Action OpaqueHook;
        public Action PhysicsHook;
		public PolylineRender Polyline;
		public SystemLighting SystemLighting = new SystemLighting();
        public ParticleEffectPool FxPool;
        public BeamsBuffer Beams;
        public QuadBuffer QuadBuffer;
        public DfmDrawMode DfmMode = DfmDrawMode.Normal;
        public RenderContext RenderContext => rstate;
		RenderContext rstate;
		Game game;
		Texture2D dot;

        public int ZoneVersion = 0;

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

        public IRendererSettings Settings;
        Billboards billboards;
        ResourceManager resman;


        public ResourceManager ResourceManager
        {
            get
            {
                return resman;
            }
        }

        public SystemRenderer(ICamera camera, GameResourceManager resources, Game game)
        {
            this.game = game;
            Settings = game.GetService<IRendererSettings>();
            billboards = game.GetService<Billboards>();
            Commands = game.GetService<CommandBuffer>();
            this.camera = camera;
            AsteroidFields = new List<AsteroidFieldRenderer>();
            Nebulae = new List<NebulaRenderer>();
            StarSphereModels = new RigidModel[0];
            FxPool = new ParticleEffectPool(resources.GLWindow.RenderContext, Commands);
            rstate = resources.GLWindow.RenderContext;
            resman = resources;
            Polyline = new PolylineRender(rstate, Commands);
            QuadBuffer = new QuadBuffer(rstate);
            dot = (Texture2D)resources.FindTexture(ResourceManager.WhiteTextureName);
            DebugRenderer = new LineRenderer(rstate);
            Beams = new BeamsBuffer(resources, rstate);
		}

        public void LoadZones(IList<AsteroidField> asteroids, IList<Nebula> nebulae)
        {
            if (AsteroidFields != null)
                foreach (var f in AsteroidFields) f.Dispose();
            AsteroidFields = new List<AsteroidFieldRenderer>();
            Nebulae = new List<NebulaRenderer>();
            if (asteroids != null)
            {
                foreach(var field in asteroids)
                    AsteroidFields.Add(new AsteroidFieldRenderer(field, this));
            }

            if (nebulae != null)
            {
                foreach(var n in nebulae)
                    Nebulae.Add(new NebulaRenderer(n, Game, this));
            }
        }

        public void LoadStarspheres(StarSystem system)
        {
            starSystem = system;
            if (StarSphereModels != null)
            {
                StarSphereModels = new RigidModel[0];
            }
            List<RigidModel> starSphereRenderData = new List<RigidModel>();
            void AddModel(ResolvedModel mdl)
            {
                if (mdl == null) return;
                var loaded = (mdl.LoadFile(resman)?.Drawable as IRigidModelFile);
                if (loaded == null) return;
                starSphereRenderData.Add(loaded.CreateRigidModel(true, resman));
            }
            AddModel(system.StarsBasic);
            AddModel(system.StarsComplex);
            AddModel(system.StarsNebula);

            StarSphereModels = starSphereRenderData.ToArray();
        }

        public void LoadLights(StarSystem system)
        {
            SystemLighting = new SystemLighting();
            SystemLighting.Ambient = new Color4(system.AmbientColor, 1);
            foreach (var lt in system.LightSources)
                SystemLighting.Lights.Add(new DynamicLight() { Light = lt.Light });
        }


		public void LoadSystem(StarSystem system)
		{
            LoadLights(system);
            LoadStarspheres(system);
            LoadZones(system.AsteroidFields, system.Nebulae);
        }

		public void Update(double elapsed)
        {
            foreach (var model in StarSphereModels)
                model.Update(game.TotalTime);
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
				if (n.Nebula.Zone.ContainsPoint(position))
					return n;
			}
			return null;
		}

		NebulaRenderer CheckNebulae()
        {
            if (!DrawNebulae) return null;
			for (int i = 0; i < Nebulae.Count; i++)
			{
				var n = Nebulae[i];
				if (n.Nebula.Zone.ContainsPoint(camera.Position))
					return n;
			}
			return null;
		}

		MultisampleTarget msaa;
		int _mwidth = -1, _mheight = -1;
        public CommandBuffer Commands;
		int _twidth = -1, _theight = -1;
		int _dwidth = -1, _dheight = -1;
		DepthMap depthMap;

        public List<ObjectRenderer> objects = new List<ObjectRenderer>(250);

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
		public unsafe void Draw(int renderWidth, int renderHeight)
		{
            if (renderWidth == 0 || renderHeight == 0)
                //Don't render on Width/Height = 0
                return;
            RenderTarget restoreTarget = rstate.RenderTarget;
			if (Settings.SelectedMSAA > 0)
			{
				if (_mwidth != renderWidth || _mheight != renderHeight)
				{
					_mwidth = renderWidth;
                    _mheight = renderHeight;
                    if (msaa != null) {
                        msaa.Dispose();
                    }
                    msaa = new MultisampleTarget(rstate, renderWidth, renderHeight, Settings.SelectedMSAA);
                }
                rstate.PushViewport(new Rectangle(0, 0, renderWidth, renderHeight));
                rstate.PushScissor(new Rectangle(0, 0, renderWidth, renderHeight), false);
                rstate.RenderTarget = msaa;
			}
            rstate.PreferredFilterLevel = Settings.SelectedFiltering;
            rstate.AnisotropyLevel = Settings.SelectedAnisotropy;
			NebulaRenderer nr = CheckNebulae(); //are we in a nebula?
            rstate.SetCamera(camera);
            Commands.Camera = camera;
			bool transitioned = false;
			if (nr != null)
				transitioned = nr.FogTransitioned() && DrawNebulae;
			rstate.DepthEnabled = true;
            Commands.BonesMax = Commands.BonesOffset = 0;
            Commands.BonesBuffer.BeginStreaming();
            QuadBuffer.BeginUpload();
            foreach (var obj in tempFx)
            {
                obj.Render.PrepareRender(camera, nr, this, false);
            }
            for (int i = 0; i < World.Objects.Count; i++)
            {
                World.Objects[i].PrepareRender(camera, nr, this);
            }
            foreach(var n in Nebulae)
                n.UploadPuffs();
            QuadBuffer.EndUpload();
            Commands.BonesBuffer.EndStreaming(Commands.BonesMax);
			if (transitioned)
			{
				//Fully in fog. Skip Starsphere
				rstate.ClearColor = nr.Nebula.FogColor;
				rstate.ClearAll();
			}
			else
            {
                rstate.ClearColor =
                    BackgroundOverride ??
                    starSystem?.BackgroundColor ??
                    NullColor;
                rstate.ClearAll();
            }
			DebugRenderer.StartFrame(rstate);
			Commands.StartFrame(rstate);
            FxPool.StartFrame(camera, Polyline);
			rstate.DepthEnabled = true;
			//Optimisation for dictionary lookups
			LightEquipRenderer.FrameStart();
			//Clear depth buffer for game objects
			billboards.Begin(camera, Commands);
            SystemLighting.NumberOfTilesX = -1;
            //Simple depth pre-pass
            rstate.ColorWrite = false;
            rstate.DepthFunction = DepthFunction.Less;
            foreach (var obj in objects) obj.DepthPrepass(camera, rstate);
            rstate.DepthFunction = DepthFunction.LessEqual;
            rstate.ColorWrite = true;
			//Actual Drawing

            Beams.Begin(Commands, resman, camera);
			foreach (var obj in objects) obj.Draw(camera, Commands, SystemLighting, nr);
            Beams.End();
			for (int i = 0; i < AsteroidFields.Count; i++) AsteroidFields[i].Draw(resman, SystemLighting, Commands, nr);
            if (DrawNebulae)
            {
                if (nr == null)
                {
                    for (int i = 0; i < Nebulae.Count; i++) Nebulae[i].Draw(Commands);
                }
                else
                    nr.Draw(Commands);
            }
            billboards.End();
			FxPool.EndFrame();
			//Opaque Pass
			rstate.DepthEnabled = true;
			Commands.DrawOpaque(rstate);
            if ((!transitioned || !DrawNebulae) && DrawStarsphere)
            {
                //Starsphere
                rstate.DepthRange = new Vector2(1, 1);
                if (camera is ThnCamera thn && !ZOverride) {
                    thn.DefaultZ();
                    rstate.SetCamera(thn);
                }
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
                        var w = p.LocalTransform.Matrix() * ssworld;
                        var bsphere = new BoundingSphere(Vector3.Transform(p.Mesh.Center, w), p.Mesh.Radius);
                        if (camera.FrustumCheck(bsphere))
                            p.Mesh.DrawImmediate(0, resman, rstate, w, ref lighting, mdl.MaterialAnims, BasicMaterial.ForceAlpha);
                    }
                }
                if (camera is ThnCamera thn2 && !ZOverride) {
                    thn2.CameraZ();
                    rstate.SetCamera(thn2);
                }
                if (nr != null && DrawNebulae)
                {
                    //rstate.DepthEnabled = false;
                    nr.RenderFogTransition();
                    //rstate.DepthEnabled = true;
                }

                rstate.DepthRange = new Vector2(0, 1);
            }
            OpaqueHook?.Invoke();
            //Transparent Pass
            rstate.DepthWrite = false;
			Commands.DrawTransparent(rstate);
			rstate.DepthWrite = true;
            rstate.DepthEnabled = true;
            PhysicsHook?.Invoke();
            foreach (var point in debugPoints)
            {
                var lX = point + new Vector3(5, 0, 0);
                var lmX = point + new Vector3(-5, 0, 0);
                var lY = point + new Vector3(0, -5, 0);
                var lmY = point + new Vector3(0, 5, 0);
                var lZ = point + new Vector3(0, 0, 5);
                var lmZ = point + new Vector3(0, 0, -5);
                DebugRenderer.DrawLine(lX, lmX, Color4.Red);
                DebugRenderer.DrawLine(lY, lmY, Color4.Red);
                DebugRenderer.DrawLine(lZ, lmZ, Color4.Red);
            }
            debugPoints = Array.Empty<Vector3>();
			DebugRenderer.Render();
			if (Settings.SelectedMSAA > 0)
			{
                rstate.PopViewport();
                rstate.PopScissor();
                if (restoreTarget == null) {
                    msaa.BlitToScreen(new Point(rstate.CurrentViewport.X, rstate.CurrentViewport.Y));
                }
                else
                    msaa.BlitToRenderTarget(restoreTarget as RenderTarget2D);
                rstate.RenderTarget = restoreTarget;
			}
            rstate.DepthEnabled = true;
            objects.Clear();
        }

		public void Dispose()
		{
            if (msaa != null) {
                msaa.Dispose();
            }
			if (depthMap != null) depthMap.Dispose();
			Polyline.Dispose();
            FxPool.Dispose();
			DebugRenderer.Dispose();
            QuadBuffer.Dispose();
            Beams.Dispose();
        }
	}
}

