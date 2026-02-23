// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Graphics;
using LibreLancer.Input;
using LibreLancer.Interface;
using LibreLancer.Media;
using LibreLancer.Render;
using LibreLancer.Sounds;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Physics;
using LibreLancer.Resources;

namespace LibreLancer
{
	public class FreelancerGame : Game
    {
		public GameDataManager GameData;
        public DebugView Debug;
        public UiContext Ui;
        public CommandBuffer Commands;
		public AudioManager Audio;
		public FontManager Fonts;
		public SoundManager Sound;
        public Typewriter Typewriter;
		public GameResourceManager ResourceManager;
		public Billboards Billboards;
		public ScreenshotManager Screenshots;
        public SaveGameFolder Saves;
        public LineRenderer Lines;
		public List<string> IntroMovies;
		public bool InitialLoadComplete = false;
        public Stopwatch LoadTimer;
        public InputMap InputMap;
		GameState currentState;

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
            LoadTimer = Stopwatch.StartNew();
        }

        private void CfgOnSaved(GameConfig config)
        {
            Audio.MasterVolume = config.Settings.MasterVolume;
            Audio.Music.Volume = config.Settings.MusicVolume;
            Audio.SetVolume(SoundCategory.Sfx, config.Settings.SfxVolume);
            Audio.SetVolume(SoundCategory.Interface, config.Settings.InterfaceVolume);
            Audio.SetVolume(SoundCategory.Voice, config.Settings.VoiceVolume);
            currentState?.OnSettingsChanged();
        }

        public void ChangeState(GameState state)
		{
            Audio.StopAllSfx();
			if (currentState != null)
				currentState.Unload();
			currentState = state;
		}

        public volatile bool InisLoaded = false;
		protected override void Load()
        {
            Thread.CurrentThread.Name = "FreelancerGame UIThread";
            //Hacky but reduces load time by initing XmlSerializer
            //as well JIT'ing the lua hardwire on a background thread.
            Task.Run(() =>
            {
                new InterfaceResources().ToXml();
                LuaContext.Initialize();
            });
			//Move to stop _TSGetMainThread error on OSX
			MinimumWindowSize = new Point(640, 480);
			SetFullScreen(Config.Settings.FullScreen);
			SetVSync(Config.Settings.VSync);
            Config.Settings.RenderContext = RenderContext;
            Config.Settings.Validate();
            //Cache
            var vfs = FileSystem.FromPath(_cfg.FreelancerPath);
			ResourceManager = new GameResourceManager(this, vfs);
			//Init Audio
			FLLog.Info("Audio", "Initialising Audio");
			Audio = new AudioManager(this);
            Audio.MasterVolume = _cfg.Settings.MasterVolume;
            Audio.Music.Volume = _cfg.Settings.MusicVolume;
            Audio.SetVolume(SoundCategory.Sfx, _cfg.Settings.SfxVolume);
            Audio.SetVolume(SoundCategory.Interface, _cfg.Settings.InterfaceVolume);
            Audio.SetVolume(SoundCategory.Voice, _cfg.Settings.VoiceVolume);
			//Load data
			FLLog.Info("Game", "Loading game data");
			GameData = new GameDataManager(new GameItemDb(vfs), ResourceManager);
			IntroMovies = GameData.GetIntroMovies();
            Saves = new SaveGameFolder();
            InputMap = new InputMap(Path.Combine(GetSaveFolder(), "keymap.ini"));
            var saveLoadTask = Task.Run(() => Saves.Load(GetSaveFolder()));
            Thread GameDataLoaderThread = new Thread(() =>
            {
                GameData.LoadData(this, true,() =>
                {
                    Sound = new SoundManager(GameData, Audio, this);
                    InputMap.LoadFromKeymap(GameData.Items.Ini.Keymap, GameData.Items.Ini.KeyList);
                    InputMap.LoadMapping();
                    Services.Add(Sound);
                    InisLoaded = true;
                });
                FLLog.Info("Game", "Finished loading game data");
                saveLoadTask.Wait();
                Saves.Infocards = GameData.Items.Ini.Infocards;
                InitialLoadComplete = true;
            });
            GameDataLoaderThread.Name = "GamedataLoader";
            GameDataLoaderThread.Start();
            Task.Run(() => PhysicsWarmup.Warmup());
            //
            Fonts = new FontManager();
			Billboards = new Billboards (RenderContext);
			RenderContext.PushViewport(0, 0, Width, Height);
			Screenshots = new ScreenshotManager(this);
            Typewriter = new Typewriter(this);
            Lines = new LineRenderer(RenderContext);
            Commands = new CommandBuffer(RenderContext);
            Services.Add(Commands);
            Services.Add(Billboards);
            Services.Add(ResourceManager);
            Services.Add(Config);
            Services.Add(Config.Settings);
            Services.Add(Fonts);
            Services.Add(GameData);
            Services.Add(Sound);
            Services.Add(Typewriter);
            Debug = new DebugView(this);
            Debug.Enabled = Config.Settings.Debug;
			ChangeState(new LoadingDataState(this));
        }

        public string GetSaveFolder()
        {
            var dir = GetSaveDirectory("Librelancer");
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception e)
            {
                FLLog.Error("Save", $"Could not create save directory {dir}. {e.Message}");
            }
            return dir;
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
                currentState.Unload();
            }
			Audio.Music.Stop (0);
			Audio.Dispose ();
			Screenshots.Stop();
		}

		protected override void Update (double elapsed)
        {
			if (currentState != null)
				currentState.Update (elapsed);
            Typewriter.Update(elapsed);
        }

		const double FPS_INTERVAL = 0.25;
		double fps_updatetimer = 0;
		int drawCallsPerFrame = 0;
		protected override void Draw (double elapsed)
		{
			RenderContext.ReplaceViewport(0, 0, Width, Height);
			fps_updatetimer -= elapsed;
			if (fps_updatetimer <= 0) {
                //Title = string.Format ("LibreLancer: {0:00.00}fps/ {2:00.00}ms - {1} Drawcalls", RenderFrequency, drawCallsPerFrame, FrameTime * 1000.0);
				fps_updatetimer = FPS_INTERVAL;
			}
			RenderContext.ClearAll ();
			if (currentState != null)
				currentState.Draw (elapsed);
            Typewriter.Render();
			drawCallsPerFrame = VertexBuffer.TotalDrawcalls;
			VertexBuffer.TotalDrawcalls = 0;
        }

		void FreelancerGame_ScreenshotSave(string filename, int width, int height, Bgra8[] data)
		{
			Screenshots.Save(filename, width, height, data);
		}
    }
}
