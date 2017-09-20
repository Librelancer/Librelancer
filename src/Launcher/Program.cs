using System;
using System.IO;
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
			bool forceNoMovies = false;
			if (Environment.OSVersion.Platform != PlatformID.MacOSX &&
			   Environment.OSVersion.Platform != PlatformID.Unix)
			{
				//Check WMP for video playback
				object legacyWMPCheck = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Active Setup\Installed Components\{22d6f312-b0f6-11d0-94ab-0080c74c7e95}", "IsInstalled", null);
				if (legacyWMPCheck == null || legacyWMPCheck.ToString() != "1")
				{
					new Application().Run(new CrashWindow(
						"Uh oh!",
						"Missing Components",
						new StreamReader(typeof(Program).Assembly.GetManifestResourceStream("Launcher.WMPMessage.txt")).ReadToEnd()));
					forceNoMovies = true;
				}
			}

			if (args.Length == 1)
			{
				LaunchPath = args[0];
			}
			else
			{
				new Application().Run(new MainWindow(forceNoMovies));
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
