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
			LibreLancer.GameConfig conf = null;
#if !DEBUG
			try {
#endif
			if (LaunchPath != null)
			{
				conf = new LibreLancer.GameConfig();
				conf.FreelancerPath = LaunchPath;
				conf.ForceAngle = ForceAngle;
				conf.IntroMovies = !SkipIntroMovies;
				conf.BufferWidth = ResWidth;
				conf.BufferHeight = ResHeight;
				conf.Launch();
			}
#if !DEBUG
			}
			catch (Exception ex) {
				conf.Crashed();
				Console.Out.WriteLine("Unhandled {0}: ", ex.GetType().Name);
				Console.Out.WriteLine(ex.Message);
				Console.Out.WriteLine(ex.StackTrace);
				new Application().Run(new CrashWindow(ex));
			}
#endif
		}

    }
}
