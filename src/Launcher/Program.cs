using System;
using Eto.Forms;
namespace Launcher
{
    static class Program
    {
        public static string LaunchPath = null;
        public static bool ForceAngle = false;
		public static bool SkipIntroMovies = true;
	    public static int ResWidth = 1024;
	    public static int ResHeight = 768;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
		static void Main(string[] args)
        {
			if (args.Length == 1)
			{
				LaunchPath = args[0];
			}
			else
			{
				new Application().Run(new MainWindow());
			}
			//Actually run the game
            if(LaunchPath != null)
            {
				var conf = new LibreLancer.GameConfig ();
				conf.FreelancerPath = LaunchPath;
                conf.ForceAngle = ForceAngle;
				conf.IntroMovies = !SkipIntroMovies;
	            conf.BufferWidth = ResWidth;
	            conf.BufferHeight = ResHeight;
				conf.Launch ();
            }
        }

    }
}
