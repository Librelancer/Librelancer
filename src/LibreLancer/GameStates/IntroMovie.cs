// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Media;
namespace LibreLancer
{
	public class IntroMovie : GameState
	{
		VideoPlayer player;
		int idx = int.MaxValue;
        bool inited = false;
		public IntroMovie(FreelancerGame game, int index) : base(game)
        {
            // Skip intro movies until VideoPlayer accepts Stream objects.
            // See issue #128 for details
            /*player = new VideoPlayer(game);
			if ((inited = player.Init(game.RenderContext)) && game.IntroMovies.Count > 0)
            {
                idx = index;
                game.Keyboard.KeyDown += HandleKeyDown;
                player.PlayFile(game.IntroMovies[index]);
            }
            else
            {*/
                Leave();
            //}
		}

		public override void Draw(double delta)
		{
			if (idx != int.MaxValue)
			{
				player.Draw();
				if (!player.Playing)
				{
					Leave();
					return;
				}
				var tex = player.GetTexture();
				Game.RenderContext.Renderer2D.DrawImageStretched(tex, new Rectangle(0, 0, Game.Width, Game.Height), Color4.White);
			}
			else
				Leave();
		}

		void HandleKeyDown(KeyEventArgs args)
		{
			Leave();
		}

		void Leave()
		{
			Game.Keyboard.KeyDown -= HandleKeyDown;
			player?.Dispose();
			if ((idx + 1) >= Game.IntroMovies.Count || !inited || Game.IntroMovies.Count <= 0)
			{
				Game.ChangeState(new LoadingDataState(Game));
			}
			else
				Game.ChangeState(new IntroMovie(Game, idx + 1));
		}

		public override void Update(double delta)
		{

		}
	}
}

