// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using LibreLancer.Platforms;
using LibreLancer.Dialogs;

namespace LibreLancer
{
    abstract class PlatformEvents : IDisposable
    {
        public virtual void WndProc(ref SDL.SDL_Event e)
        {

        }

        public virtual void Poll()
        {
        }

        public abstract void Dispose();
    }

    public record MountInfo(string Name, string Path);

    public static class Platform
	{
		public static OS RunningOS;
		internal static IPlatform RunningPlatform;
        public static string OSDescription;

        public static event Action<MountInfo[]> MountsChanged;

        internal static void OnMountsChanged(MountInfo[] mounts)
        {
            MountsChanged?.Invoke(mounts);
        }

        static Platform ()
        {
            OSDescription = $"{RuntimeInformation.OSDescription} - {RuntimeInformation.ProcessArchitecture}";
			switch (Environment.OSVersion.Platform) {
			case PlatformID.Unix:
				if (Directory.Exists ("/Applications")
				    & Directory.Exists ("/System")
				    & Directory.Exists ("/Users")
				    & Directory.Exists ("/Volumes")) {
					RunningOS = OS.Mac;
                    RunningPlatform = new MacOSPlatform();
                } else {
					RunningOS = OS.Linux;
					RunningPlatform = new LinuxPlatform ();
                    //Get current distribution
                    if (File.Exists("/etc/os-release"))
                    {
                        var x = Regex.Match(File.ReadAllText("/etc/os-release"), @"^PRETTY_NAME\s*?\=(.*)$",
                            RegexOptions.Multiline);
                        if (x.Success && x.Groups[1].Length > 0)
                            OSDescription = x.Groups[1].Value.TrimStart('\"', '\'').TrimEnd('\"', '\'') + " " +
                                            OSDescription;
                    }
                    else if (Shell.HasCommand("lsb_release")) {
                        var lsbVersion = Shell.GetString("lsb_release", "-d");
                        if (!string.IsNullOrWhiteSpace(lsbVersion))
                            OSDescription = lsbVersion + " " + OSDescription;
                    }
                }
				break;
			case PlatformID.MacOSX:
				RunningOS = OS.Mac;
                RunningPlatform = new MacOSPlatform();
                break;
			default:
				RunningOS = OS.Windows;
				RunningPlatform = new Win32Platform ();
				break;
			}
            RegisterDllMap(typeof(Platform).Assembly);
		}

        internal static void Init(string sdlBackend) => RunningPlatform.Init(sdlBackend);

        internal static PlatformEvents SubscribeEvents(IUIThread mainThread) =>
            RunningPlatform.SubscribeEvents(mainThread);

        public static MountInfo[] GetMounts() => RunningPlatform.GetMounts();

        public static string GetBasePath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            var basePath = Path.GetDirectoryName(processModule?.FileName);
            return basePath;
        }

        private static bool? portable;
        public static bool IsPortable()
        {
            if (portable.HasValue) return portable.Value;
            portable =  File.Exists(Path.Combine(Path.GetDirectoryName(typeof(Platform).Assembly.Location), "portable"));
            if(portable.Value)
                FLLog.Info("Data", "Running in portable mode");
            return portable.Value;
        }

        public static string GetLocalConfigFolder() =>
            IsPortable() ? GetBasePath() : RunningPlatform.GetLocalConfigFolder();

        public static bool IsDirCaseSensitive (string directory)
		{
			return RunningPlatform.IsDirCaseSensitive (directory);
		}

        public static void RegisterDllMap(Assembly assembly)
        {
            if(RunningOS == OS.Linux ||
               RunningOS == OS.Mac)
                DllMap.Register(assembly);
        }
        public static byte[] GetMonospaceBytes()
        {
            return RunningPlatform.GetMonospaceBytes();
        }

        internal static List<string> LoadedTTFs = new List<string>();
        internal static event Action FontLoaded;
        public static void AddTtfFile(string id, byte[] ttf)
        {
            if (LoadedTTFs.Contains(id)) return;
            LoadedTTFs.Add(id);
            RunningPlatform.AddTtfFile(ttf);
            FontLoaded?.Invoke();
        }

        internal static void Shutdown()
        {
            RunningPlatform.Shutdown();
        }

        public static string GetInformationalVersion<T>()
        {
            return ((AssemblyInformationalVersionAttribute)Assembly
                .GetAssembly(typeof(T))
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0])
                .InformationalVersion;
        }
        //Make it hard to crash with a cryptic message at startup
        const string V2012_64 = "Librelancer requires Visual C++ 2012 redistributable (x64). Download from: https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe";
        const string V2012_32 = "Librelancer requires Visual C++ 2012 redistributable (x86). Download from: https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe";
        const string V2015_64 = "Librelancer requires the Microsoft Visual C++ Redistributable for Visual Studio 2015, 2017 and 2019 (x64). Download from: https://aka.ms/vs/16/release/vc_redist.x64.exe";
        const string V2015_32 = "Librelancer requires the Microsoft Visual C++ Redistributable for Visual Studio 2015, 2017 and 2019 (x86). Download from: https://aka.ms/vs/16/release/vc_redist.x86.exe";

        static bool CheckVCRun(string file, string errx64, string errx86)
        {
            if (LoadLibrary(file) == IntPtr.Zero)
            {
                CrashWindow.Run("Librelancer", "Missing Components",
                    "LoadLibrary failed for " + file + ": " + Marshal.GetLastWin32Error() + "\n" + (IntPtr.Size == 8 ? errx64 : errx86));
                return false;
            }
            return true;
        }

        [DllImport("kernel32.dll", SetLastError=true)]
        static extern IntPtr LoadLibrary(string lpLibFileName);

        public static bool CheckDependencies()
        {
            if (RunningOS != OS.Windows) return true;
            #if MSVC_BUILD
            if (!CheckVCRun("vcruntime140.dll", V2015_64, V2015_32)) return false;
            if(IntPtr.Size == 8) //vcruntime140_1.dll only seems present on x64
                if (!CheckVCRun("vcruntime140_1.dll", V2015_64, V2015_32)) return false;
            #endif
            return true;
        }
    }

	public enum OS
	{
		Windows,
		Mac,
		Linux
	}
}

