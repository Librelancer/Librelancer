// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using LibreLancer.GameData;
using LibreLancer.Media;
namespace LibreLancer
{
	public class FreelancerGame : Game
    {
		public GameDataManager GameData;
		public AudioManager Audio;
		public FontManager Fonts;
		public SoundManager Sound;
		public ResourceManager ResourceManager;
		public Renderer2D Renderer2D;
		public Billboards Billboards;
		public NebulaVertices Nebulae;
		public ScreenshotManager Screenshots;
		public List<string> IntroMovies;
		public string MpvOverride;
		public bool InitialLoadComplete = false;
		int uithread;
		bool useintromovies;
		GameState currentState;
        public ViewportManager ViewportManager;
		public Viewport Viewport {
			get {
				return new Viewport (0, 0, Width, Height);
			}
		}
		public GameConfig Config
		{
			get
			{
				return _cfg;
			}
		}
		GameConfig _cfg;
		public FreelancerGame(GameConfig config) : base(config.BufferWidth, config.BufferHeight, false)
		{
			//DO NOT RUN CODE HERE. IT CAUSES THE STUPIDEST CRASH ON OSX KNOWN TO MAN
			_cfg = config;
			ScreenshotSave += FreelancerGame_ScreenshotSave;
            Utf.Mat.TextureData.Bitch = true;
        }

		public void ChangeState(GameState state)
		{
            Audio.StopAllSfx();
			if (currentState != null)
				currentState.Unregister();
			currentState = state;
		}
		protected override void Load()
        {
			//Move to stop _TSGetMainThread error on OSX
			MinimumWindowSize = new Point(640, 480);
			SetVSync(Config.VSync);
			new IdentityCamera(this);
			uithread = Thread.CurrentThread.ManagedThreadId;
			useintromovies = _cfg.IntroMovies;
			FLLog.Info("Platform", Platform.RunningOS.ToString() + (IntPtr.Size == 4 ? " 32-bit" : " 64-bit"));
			FLLog.Info("Available Threads", Environment.ProcessorCount.ToString());
			//Cache
			ResourceManager = new ResourceManager(this);
			//Init Audio
			FLLog.Info("Audio", "Initialising Audio");
			Audio = new AudioManager(this);
			if (_cfg.MuteMusic)
				Audio.Music.Volume = 0f;
			//Load data
			FLLog.Info("Game", "Loading game data");
			GameData = new GameDataManager(_cfg.FreelancerPath, ResourceManager);
			IntroMovies = GameData.GetIntroMovies();
			MpvOverride = _cfg.MpvOverride;
            Thread GameDataLoaderThread = new Thread(() =>
            {
                GameData.LoadData();
                Sound = new SoundManager(GameData, Audio);
                FLLog.Info("Game", "Finished loading game data");
                InitialLoadComplete = true;
            });
            GameDataLoaderThread.Name = "GamedataLoader";
            GameDataLoaderThread.Start();
            //
            Renderer2D = new Renderer2D(RenderState);
			Fonts = new FontManager(this);
			Billboards = new Billboards ();
			Nebulae = new NebulaVertices();
			ViewportManager = new ViewportManager (RenderState);
			ViewportManager.Push(0, 0, Width, Height);
			Screenshots = new ScreenshotManager(this);

            Services.Add(Billboards);
            Services.Add(Nebulae);
            Services.Add(ResourceManager);
            Services.Add(Renderer2D);
            Services.Add(Config);

			if (useintromovies && IntroMovies.Count > 0)
				ChangeState(new IntroMovie(this, 0));
			else
				ChangeState(new LoadingDataState(this));
        }

        protected override void OnResize()
        {
            if (currentState != null)
                currentState.OnResize();
        }

		protected override void Cleanup()
		{
            if (currentState != null)
                currentState.Unregister();
			Audio.Music.Stop ();
			Audio.Dispose ();
			Screenshots.Stop();
		}

		protected override void Update (double elapsed)
		{
			if (currentState != null)
				currentState.Update (TimeSpan.FromSeconds (elapsed));
		}

		const double FPS_INTERVAL = 0.25;
		double fps_updatetimer = 0;
		int drawCallsPerFrame = 0;
		protected override void Draw (double elapsed)
		{
			ViewportManager.Instance.Replace(0, 0, Width, Height);
			fps_updatetimer -= elapsed;
			if (fps_updatetimer <= 0) {
				Title = string.Format ("LibreLancer: {0:00.00}fps/ {2:00.00}ms - {1} Drawcalls", RenderFrequency, drawCallsPerFrame, FrameTime * 1000.0);
				fps_updatetimer = FPS_INTERVAL;
			}
			RenderState.ClearAll ();
			if (currentState != null)
				currentState.Draw (TimeSpan.FromSeconds (elapsed));
			drawCallsPerFrame = VertexBuffer.TotalDrawcalls;
			VertexBuffer.TotalDrawcalls = 0;
			ViewportManager.Instance.CheckViewports ();
        }

		void FreelancerGame_ScreenshotSave(string filename, int width, int height, byte[] data)
		{
			Screenshots.Save(filename, width, height, data);
		}
    }
}
