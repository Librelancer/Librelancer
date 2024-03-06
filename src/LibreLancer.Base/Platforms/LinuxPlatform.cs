// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;
using LibreLancer.Platforms.Linux;

namespace LibreLancer.Platforms
{
	class LinuxPlatform : IPlatform
	{
        [DllImport("libgtk-3.so.0")]
        static extern bool gtk_init_check(IntPtr argc, IntPtr argv);

        [DllImport("libX11.so.6")]
        static extern int XInitThreads();

		IntPtr fcconfig;

        private string tempFontsDirectory;

		public LinuxPlatform()
        {
            fcconfig = LibFontConfig.FcInitLoadConfigAndFonts();
            LibFontConfig.FcConfigSetCurrent(fcconfig);
            tempFontsDirectory = Directory.CreateTempSubdirectory("librelancer").FullName;
        }

        public void Init(string sdlBackend)
        {
            if ("x11".Equals(sdlBackend, StringComparison.OrdinalIgnoreCase))
                XInitThreads();
            gtk_init_check(IntPtr.Zero, IntPtr.Zero);
        }

        public string GetLocalConfigFolder()
        {
            string osConfigDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (String.IsNullOrEmpty(osConfigDir))
            {
                osConfigDir = Environment.GetEnvironmentVariable("HOME");
                if (String.IsNullOrEmpty(osConfigDir))
                {
                    return Environment.CurrentDirectory;
                }
                osConfigDir += "/.config/";
            }
            return osConfigDir;
        }

        public bool IsDirCaseSensitive (string directory)
		{
			return true;
		}

        //TODO: This is a wrapper around FontConfig, add to FontConfigSharp
        [DllImport("pangogame")]
        static extern void pg_addttfglobal(IntPtr file);

        public void AddTtfFile(byte[] ttf)
        {
            if (!string.IsNullOrWhiteSpace(tempFontsDirectory))
            {
                var path = Path.Combine(tempFontsDirectory, Path.GetRandomFileName() + ".ttf");
                File.WriteAllBytes(path, ttf);
                var str = UnsafeHelpers.StringToHGlobalUTF8(path);
                pg_addttfglobal(str);
                Marshal.FreeHGlobal(str);
            }
        }

        static LibFontConfig.FcResult GetString(IntPtr pattern, string obj, int n, ref string val)
        {
            var ptr = IntPtr.Zero;
            var result =  LibFontConfig.FcPatternGetString (pattern, obj, n, ref ptr);
            if (result == LibFontConfig.FcResult.Match)
                val = Marshal.PtrToStringAnsi (ptr);
            return result;
        }
        public byte[] GetMonospaceBytes()
        {
            var pat = LibFontConfig.FcNameParse("monospace");
            LibFontConfig.FcConfigSubstitute(fcconfig, pat, LibFontConfig.FcMatchKind.Pattern);
            LibFontConfig.FcDefaultSubstitute(pat);
            var fnt = LibFontConfig.FcFontMatch(fcconfig, pat, out _);
            string file = null;
            bool use = GetString(fnt, LibFontConfig.FC_FILE, 0, ref file) == LibFontConfig.FcResult.Match;
            LibFontConfig.FcPatternDestroy(pat);
            if (use)
                return System.IO.File.ReadAllBytes(file);
            else
                throw new Exception("No system monospace font found");
        }

        public PlatformEvents SubscribeEvents(IUIThread mainThread)
        {
            var ev = new GLib.GMountEvents(mainThread);
            ev.Start();
            return ev;
        }

        public MountInfo[] GetMounts() => GLib.GetMounts();

        public void Shutdown()
        {
            try
            {
                if(!string.IsNullOrWhiteSpace(tempFontsDirectory))
                    Directory.Delete(tempFontsDirectory, true);
            }
            catch (Exception)
            {
                // ignored
            }

            tempFontsDirectory = null;
        }
    }
}

