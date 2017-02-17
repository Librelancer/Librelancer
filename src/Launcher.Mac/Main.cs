using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using AppKit;
using Foundation;
namespace Launcher.Mac
{
	static class MainClass
	{
		public static string LaunchPath = null;
		public static bool SkipIntroMovies;
		public static bool MuteMusic;
		static void Main(string[] args)
		{
			NSApplication.Init();
			bundlecfg cfg = null;
			string bundlepath;
			using (var pool = new NSAutoreleasePool())
			{
				var del = new AppDelegate();
				NSApplication.SharedApplication.Delegate = del;
				NSApplication.SharedApplication.Run();
				string path = NSBundle.MainBundle.PathForResource("bundlecfg", "xml");
				bundlepath = NSBundle.MainBundle.BundlePath;
				if (File.Exists(path))
				{
					try
					{
						var slz = new XmlSerializer(typeof(bundlecfg));
						cfg = (bundlecfg)slz.Deserialize(File.OpenRead(path));
					}
					catch (Exception)
					{

					}
				}
			}
			if (LaunchPath != null)
			{
				//HACK: Work-around Xamarin.Mac getting itself confused with all the dependencies
				var asm = Assembly.LoadFrom(bundlepath + "/Contents/MonoBundle/LibreLancer.dll");
				var t = asm.GetType("LibreLancer.GameConfig", true);
				dynamic conf = Activator.CreateInstance(t);
				//var conf = new LibreLancer.GameConfig();
				if (cfg != null)
					if (cfg.mpv != null)
					conf.MpvOverride = Path.Combine(bundlepath, cfg.mpv);
				conf.IntroMovies = !SkipIntroMovies;
				conf.MuteMusic = MuteMusic;
				conf.FreelancerPath = LaunchPath;
				conf.Launch();
			}
		}
	}
	public class bundlecfg
	{
		public string mpv;
	}
}
