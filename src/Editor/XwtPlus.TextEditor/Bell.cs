using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace XwtPlus.TextEditor
{
	static class Bell
	{
		static bool Canberra;
		static bool Win32;

		static Bell()
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32S ||
			   Environment.OSVersion.Platform == PlatformID.Win32NT ||
			   Environment.OSVersion.Platform == PlatformID.Win32Windows)
			{
				Win32 = true;
				return;
			}
			if (File.Exists("/usr/bin/canberra-gtk-play"))
			{
				Canberra = true;
				return;
			}
		}
		[DllImport("user32.dll")]
		static extern bool MessageBeep(uint type);
		public static void Play()
		{
			if (Canberra)
			{
				Process.Start("/usr/bin/canberra-gtk-play", "--id='bell'");
			}
			else if (Win32)
			{
				MessageBeep(0);
			}
			else
			{
				Console.Write('\a');
			}
		}
	}
}
