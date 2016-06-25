using AppKit;
using Foundation;
namespace Launcher.Mac
{
	static class MainClass
	{
		public static string LaunchPath = null;
		static void Main(string[] args)
		{
			NSApplication.Init();
			using (var pool = new NSAutoreleasePool())
			{
				var del = new AppDelegate();
				NSApplication.SharedApplication.Delegate = del;
				NSApplication.SharedApplication.Run();
			}
			if (LaunchPath != null)
			{
				var conf = new LibreLancer.GameConfig();
				conf.FreelancerPath = LaunchPath;
				conf.Launch();
			}
		}
	}
}
