using System;
using LibreLancer.Media;
namespace LibreLancer
{
	public class IntroMovie : GameState
	{
		VideoPlayer player;
		int idx;

		public IntroMovie(FreelancerGame game, int index) : base(game)
		{
			idx = index;
			player = new VideoPlayer(game);
			if (!player.Init())
				game.ChangeState(new LoadingDataState(game));
			index = idx;
			game.Keyboard.KeyDown += HandleKeyDown;
			player.PlayFile(game.IntroMovies[index]);
		}

		public override void Draw(TimeSpan delta)
		{
			player.Draw();
			if (!player.Playing)
			{
				Leave();
			}
			var tex = player.GetTexture();
			Game.Renderer2D.Start(Game.Width, Game.Height);
			Game.Renderer2D.DrawImageStretched(tex, new Rectangle(0, 0, Game.Width, Game.Height), Color4.White);
			Game.Renderer2D.Finish();
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

