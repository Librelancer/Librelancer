// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;
using LibreLancer;
using LibreLancer.Dialogs;
namespace LancerEdit
{
	class Program
	{
        [DllImport("kernel32.dll")]
        static extern bool SetDllDirectory(string directory);

        [STAThread]
        static void Main(string[] args)
		{

            if (Platform.RunningOS == OS.Windows)
            {
                string bindir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                var fullpath = Path.Combine(bindir, IntPtr.Size == 8 ? "x64" : "x86");
                SetDllDirectory(fullpath);
            }
            if (!Platform.CheckDependencies()) return;
            MainWindow mw = null;
#if !DEBUG
            var domain = AppDomain.CurrentDomain;
            domain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                var ex = (Exception)(e.ExceptionObject);
                CrashWindow.Run("Uh-oh!", "LancerEdit has crashed. See the log for more information.",
                ex.Message + "\n" + ex.StackTrace);
            };
            try
            {
#endif
            mw = new MainWindow() { InitOpenFile = args };
            mw.Run();
#if !DEBUG
            }
            catch(Exception ex)
            {
                try { mw.Crashed();  } catch { }
                CrashWindow.Run("Uh-oh!", "LancerEdit has crashed. See the log for more information.", ex.Message + "\n" + ex.StackTrace);
            }
#endif
        }
	}
}
