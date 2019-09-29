// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
                FormatException(ex));
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
                CrashWindow.Run("Uh-oh!", "Librelancer has crashed. See the log for more information.", FormatException(ex));
            }
#endif
        }

        static string FormatException(Exception ex)
        {
            var builder = new StringBuilder();
            builder.AppendLine(ex.Message);
            builder.AppendLine(ex.StackTrace);
            Exception ex2 = ex;
            while ((ex2 = ex2.InnerException) != null)
            {
                builder.AppendLine($"Inner: {ex2.Message}");
                builder.AppendLine(ex2.StackTrace);
            }
            return builder.ToString();
        }
    }
}
