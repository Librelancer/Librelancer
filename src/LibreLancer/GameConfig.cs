using System;

namespace LibreLancer
{
	public class GameConfig
	{
		public string FreelancerPath;
		public GameConfig ()
		{
		}

		public void Launch()
		{
			using (var game = new FreelancerGame (this)) {
				game.Run (60.0, 60.0);
			}
		}
	}
}

