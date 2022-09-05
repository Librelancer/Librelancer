// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Media
{
	public class VideoPlayer : IDisposable
	{
		VideoPlayerInternal player;
		public bool Playing
		{
			get
			{
				return player.Playing;
			}
		}
		public VideoPlayer(Game game)
        {
            if (Platform.RunningOS != OS.Windows)
                player = null;
            else
                LoadWMF();
        }

		void LoadWMF()
		{
			player = new VideoPlayerWMF();
		}
		public bool Init()
		{
			if (player == null)
				return false;
			return player.Init();
		}
		public void PlayFile(string filename)
		{
            if (player != null)
                player.PlayFile(filename);
		}
		public void Draw(RenderContext rstate)
		{
            if (player != null)
                player.Draw(rstate);
		}
		public void Dispose()
		{
            if(player != null)
			    player.Dispose();
		}
		public Texture2D GetTexture()
		{
			return player.GetTexture();
		}
	}
}

