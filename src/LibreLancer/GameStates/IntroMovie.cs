using System;
using LibreLancer.Media;
namespace LibreLancer
{
	public class IntroMovie : GameState
	{
		VideoPlayer player;
		int idx = int.MaxValue;

		public IntroMovie(FreelancerGame game, int index) : base(game)
		{
			player = new VideoPlayer(game, game.MpvOverride);
			if (player.Init())
			{
				idx = index;
				game.Keyboard.KeyDown += HandleKeyDown;
				player.PlayFile(game.IntroMovies[index]);
			}
		}

		public override void Draw(TimeSpan delta)
		{
			if (idx != int.MaxValue)
			{
				player.Draw(Game.RenderState);
				if (!player.Playing)
				{
					Leave();
					return;
				}
				var tex = player.GetTexture();
				Game.Renderer2D.Start(Game.Width, Game.Height);
				Game.Renderer2D.DrawImageStretched(tex, new Rectangle(0, 0, Game.Width, Game.Height), Color4.White);
				Game.Renderer2D.Finish();
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
			player.Dispose();
			if ((idx + 1) >= Game.IntroMovies.Count)
			{
				Game.ChangeState(new LoadingDataState(Game));
			}
			else
				Game.ChangeState(new IntroMovie(Game, idx + 1));
		}

		public override void Update(TimeSpan delta)
		{
			
		}
	}
}

