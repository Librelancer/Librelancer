using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibreLancer
{
	public class GameConfig
	{
		public string FreelancerPath;
		public GameConfig ()
		{
		}

		[DllImport("kernel32.dll")]
		static extern bool SetDllDirectory (string directory);

		public void Launch()
		{
			if (Platform.RunningOS == OS.Windows) {
				string bindir = Path.GetDirectoryName (typeof(GameConfig).Assembly.Location);
				var fullpath = Path.Combine (bindir, IntPtr.Size == 8 ? "win64" : "win32");
				SetDllDirectory (fullpath);
			}
			using (var game = new FreelancerGame (this)) {
				game.Run (60.0, 60.0);
			}
		}
	}
}

