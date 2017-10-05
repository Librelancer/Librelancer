using System;
using System.IO;
using Eto.Forms;
namespace Launcher
{
	static class Program
	{
		public static LibreLancer.GameConfig Config;
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

			Config = LibreLancer.GameConfig.Create();
			var app = new Application();
			var win = new MainWindow(forceNoMovies);
			app.Run(win);
			//Actually run the game
#if !DEBUG
			try {
#endif
			if (win.Run && Config.FreelancerPath != null)
			{
				Config.Save();
				Config.Launch();
			}
#if !DEBUG
			}
			catch (Exception ex) {
				Config.Crashed();
				Console.Out.WriteLine("Unhandled {0}: ", ex.GetType().Name);
				Console.Out.WriteLine(ex.Message);
				Console.Out.WriteLine(ex.StackTrace);
				new Application().Run(new CrashWindow(ex));
			}
#endif
		}

    }
}
