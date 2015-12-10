using System;

namespace LibreLancer
{
	public abstract class GameState
	{
		protected FreelancerGame Game;
		public GameState (FreelancerGame game)
		{
			Game = game;
		}
		public abstract void Update(TimeSpan delta);
		public abstract void Draw(TimeSpan delta);
	}
}

