using System;
using System.IO;
using System.Xml.Serialization;
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
				var conf = new LibreLancer.GameConfig();
				if (cfg != null)
					if (cfg.mpv != null)
					conf.MpvOverride = Path.Combine(bundlepath, cfg.mpv);
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
