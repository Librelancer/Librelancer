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
using System.Threading;
using System.IO;
using LibreLancer.GameData;
using LibreLancer.Media;
namespace LibreLancer
{
	public class FreelancerGame : Game
    {
		public LegacyGameData GameData;
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
		public FreelancerGame(GameConfig config) : base(config.BufferWidth, config.BufferHeight, false, config.ForceAngle)
		{
			//DO NOT RUN CODE HERE. IT CAUSES THE STUPIDEST CRASH ON OSX KNOWN TO MAN
			_cfg = config;
			ScreenshotSave += FreelancerGame_ScreenshotSave;
        }

		public void ChangeState(GameState state)
		{
			if (currentState != null)
				currentState.Unregister();
			currentState = state;
		}
		protected override void Load()
        {
			//Move to stop _TSGetMainThread error on OSX
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
			GameData = new LegacyGameData(_cfg.FreelancerPath, ResourceManager);
			IntroMovies = GameData.GetIntroMovies();
			MpvOverride = _cfg.MpvOverride;
			new Thread(() =>
			{
				GameData.LoadData();
				Sound = new SoundManager(GameData, Audio);
				FLLog.Info("Game", "Finished loading game data");
				InitialLoadComplete = true;
			}).Start();
			//
			Renderer2D = new Renderer2D(RenderState);
			Fonts = new FontManager(this);
			Billboards = new Billboards ();
			Nebulae = new NebulaVertices();
			var vp = new ViewportManager (RenderState);
			vp.Push (0, 0, Width, Height);
			Screenshots = new ScreenshotManager(this);
			if (useintromovies && IntroMovies.Count > 0)
				ChangeState(new IntroMovie(this, 0));
			else
				ChangeState(new LoadingDataState(this));
        }

		protected override void Cleanup()
		{
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
