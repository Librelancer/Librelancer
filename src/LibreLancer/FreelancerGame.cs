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
		public SoundManager Sound;
		public ResourceManager ResourceManager;
		public RenderState RenderState;
		public Renderer2D Renderer2D;
		public Billboards Billboards;
		public NebulaVertices Nebulae;
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
		public FreelancerGame(GameConfig config) : base(1024, 768, false)
        {
			//Setup
			uithread = Thread.CurrentThread.ManagedThreadId;
			useintromovies = config.IntroMovies;
			FLLog.Info("Platform", Platform.RunningOS.ToString() + (IntPtr.Size == 4 ? " 32-bit" : " 64-bit"));
			//Cache
			ResourceManager = new ResourceManager(this);
			//Init Audio
			FLLog.Info("Audio", "Initialising Audio");
			Audio = new AudioManager();
			if(config.MuteMusic)
				Audio.Music.Volume = 0f;
			//Load data
			FLLog.Info("Game", "Loading game data");
			GameData = new LegacyGameData(config.FreelancerPath, ResourceManager);
			IntroMovies = GameData.GetIntroMovies();
			MpvOverride = config.MpvOverride;
			new Thread(() => {
				GameData.LoadData();
				Sound = new SoundManager(GameData, Audio);
				FLLog.Info("Game", "Finished loading game data");
				InitialLoadComplete = true;
			}).Start ();

        }

		public void ChangeState(GameState state)
		{
			currentState = state;
		}
		protected override void Load()
        {
			RenderState = new RenderState ();
			Renderer2D = new Renderer2D(RenderState);
			Billboards = new Billboards ();
			Nebulae = new NebulaVertices();
			var vp = new ViewportManager (RenderState);
			vp.Push (0, 0, Width, Height);
			if (useintromovies)
				ChangeState(new IntroMovie(this, 0));
			else
				ChangeState(new LoadingDataState(this));
        }

		protected override void Cleanup()
		{
			Audio.Music.Stop ();
			Audio.Dispose ();
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
			fps_updatetimer -= elapsed;
			if (fps_updatetimer <= 0) {
				Title = string.Format ("LibreLancer: {0:#.##}fps / {1} Drawcalls", RenderFrequency, drawCallsPerFrame);
				fps_updatetimer = FPS_INTERVAL;
			}
			RenderState.ClearAll ();
			if (currentState != null)
				currentState.Draw (TimeSpan.FromSeconds (elapsed));
			drawCallsPerFrame = VertexBuffer.TotalDrawcalls;
			VertexBuffer.TotalDrawcalls = 0;
			ViewportManager.Instance.CheckViewports ();
        }
    }
}
