// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;
using LibreLancer;
using LibreLancer.Dialogs;
namespace lancer
{
    class MainClass
    {
        [DllImport("kernel32.dll")]
        static extern bool SetDllDirectory(string directory);

        [STAThread]
        public static void Main(string[] args)
        {
            if (Platform.RunningOS == OS.Windows)
            {
                string bindir = Path.GetDirectoryName(typeof(MainClass).Assembly.Location);
                var fullpath = Path.Combine(bindir, IntPtr.Size == 8 ? "x64" : "x86");
                SetDllDirectory(fullpath);
            }
            if (!Platform.CheckDependencies()) return;
            FreelancerGame flgame = null;
#if !DEBUG
            var domain = AppDomain.CurrentDomain;
            domain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                var ex = (Exception)(e.ExceptionObject);
                CrashWindow.Run("Uh-oh!", "Librelancer has crashed. See the log for more information.", 
                ex.Message + "\n" + ex.StackTrace);
            };
            try {
#endif
            Func<string> filePath = null;
            if(args.Length > 0)
                filePath = () => args[0];
            var cfg = GameConfig.Create(true, filePath);
            flgame = new FreelancerGame(cfg);
            flgame.Run();
#if !DEBUG
            }
            catch (Exception ex)
            {
                try { flgame.Crashed();  } catch { }
                CrashWindow.Run("Uh-oh!", "Librelancer has crashed. See the log for more information.", ex.Message + "\n" + ex.StackTrace);
            }
#endif
        }
    }
}
