// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using LibreLancer.GameData;
using LibreLancer.Interface;
using LibreLancer.Media;
namespace LibreLancer
{
	public class FreelancerGame : Game
    {
		public GameDataManager GameData;
        public DebugView Debug;
        public UiContext Ui;
		public AudioManager Audio;
		public FontManager Fonts;
		public SoundManager Sound;
        public Typewriter Typewriter;
		public GameResourceManager ResourceManager;
		public Billboards Billboards;
		public NebulaVertices Nebulae;
		public ScreenshotManager Screenshots;
        public SaveGameFolder Saves;
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
            _cfg.Saved += CfgOnSaved;
			ScreenshotSave += FreelancerGame_ScreenshotSave;
            Utf.Mat.TextureData.Bitch = true;
        }

        private void CfgOnSaved(GameConfig config)
        {
            Audio.MasterVolume = config.Settings.MasterVolume;
            Audio.Music.Volume = config.Settings.MusicVolume;
        }

        public void ChangeState(GameState state)
		{
            Audio.ReleaseAllSfx();
			if (currentState != null)
				currentState.Unregister();
			currentState = state;
		}

        public bool InisLoaded = false;
		protected override void Load()
        {
            Thread.CurrentThread.Name = "FreelancerGame UIThread";
			//Move to stop _TSGetMainThread error on OSX
			MinimumWindowSize = new Point(640, 480);
			SetVSync(Config.Settings.VSync);
            Config.Settings.RenderContext = RenderContext;
			new IdentityCamera(this);
			uithread = Thread.CurrentThread.ManagedThreadId;
			useintromovies = _cfg.IntroMovies;
			FLLog.Info("Platform", Platform.RunningOS.ToString() + (IntPtr.Size == 4 ? " 32-bit" : " 64-bit"));
			FLLog.Info("Available Threads", Environment.ProcessorCount.ToString());
			//Cache
			ResourceManager = new GameResourceManager(this);
			//Init Audio
			FLLog.Info("Audio", "Initialising Audio");
			Audio = new AudioManager(this);
            Audio.WaitReady();
            Audio.MasterVolume = _cfg.Settings.MasterVolume;
            Audio.Music.Volume = _cfg.Settings.MusicVolume;
			//Load data
			FLLog.Info("Game", "Loading game data");
			GameData = new GameDataManager(_cfg.FreelancerPath, ResourceManager);
			IntroMovies = GameData.GetIntroMovies();
			MpvOverride = _cfg.MpvOverride;
            Saves = new SaveGameFolder();
            var saveLoadTask = Task.Run(() => Saves.Load(GetSaveFolder()));
            Thread GameDataLoaderThread = new Thread(() =>
            {
                GameData.LoadData(this, () =>
                {
                    Sound = new SoundManager(GameData, Audio);
                    Services.Add(Sound);
                    InisLoaded = true;
                });
                FLLog.Info("Game", "Finished loading game data");
                saveLoadTask.Wait();
                Saves.Infocards = GameData.Ini.Infocards;
                InitialLoadComplete = true;
            });
            GameDataLoaderThread.Name = "GamedataLoader";
            GameDataLoaderThread.Start();
            //
            Fonts = new FontManager();
			Billboards = new Billboards ();
			Nebulae = new NebulaVertices();
			ViewportManager = new ViewportManager (RenderContext);
			ViewportManager.Push(0, 0, Width, Height);
			Screenshots = new ScreenshotManager(this);
            Typewriter = new Typewriter(this);
            
            Services.Add(Billboards);
            Services.Add(Nebulae);
            Services.Add(ResourceManager);
            Services.Add(Config);
            Services.Add(Fonts);
            Services.Add(GameData);
            Services.Add(Sound);
            Services.Add(Typewriter);
            Debug = new DebugView(this);
			if (useintromovies && IntroMovies.Count > 0)
				ChangeState(new IntroMovie(this, 0));
			else
				ChangeState(new LoadingDataState(this));
        }

        public string GetSaveFolder()
        {
            return GetSaveDirectory("Librelancer", "Librelancer");
        }

        protected override void OnResize()
        {
            if (currentState != null)
                currentState.OnResize();
        }

		protected override void Cleanup()
		{
            if (currentState != null)
            {
                currentState.Exiting();
                currentState.Unregister();
            }
			Audio.Music.Stop ();
			Audio.Dispose ();
			Screenshots.Stop();
		}

		protected override void Update (double elapsed)
		{
			if (currentState != null)
				currentState.Update (elapsed);
            Typewriter.Update(elapsed);
            Audio.Update();
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
			RenderContext.ClearAll ();
			if (currentState != null)
				currentState.Draw (elapsed);
            Typewriter.Render();
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
