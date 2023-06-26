using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using LibreLancer.Dialogs;

namespace LibreLancer
{
    public static class AppHandler
    {
        [DllImport("kernel32.dll")]
        static extern bool SetDllDirectory(string directory);

        public static string ProjectName = "Librelancer";

        static void LogPlatform()
        {
            FLLog.Info("Platform", Platform.OSDescription);
            FLLog.Info("Available Threads", Environment.ProcessorCount.ToString());
        }
        
        public static void ConsoleInit(bool silent = false)
        {
            if(!silent)
                LogPlatform();
            if (Platform.RunningOS == OS.Windows)
            {
                string bindir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                var fullpath = Path.Combine(bindir, IntPtr.Size == 8 ? "x64" : "x86");
                SetDllDirectory(fullpath);
            }
        }
        
        public static void Run(Action action, Action onCrash = null)
        {
            LogPlatform();
            string errorMessage =  $"{ProjectName} has crashed. See the log for more information.";
            Environment.SetEnvironmentVariable("ALSOFT_LOGLEVEL", "2");
            if (Platform.RunningOS == OS.Windows)
            {
                string bindir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                var fullpath = Path.Combine(bindir, IntPtr.Size == 8 ? "x64" : "x86");
                SetDllDirectory(fullpath);
                //Setup Spew
                var spewFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProjectName);
                if (!Directory.Exists(spewFolder)) Directory.CreateDirectory(spewFolder);
                string spewFilename = Assembly.GetCallingAssembly().GetName().Name + ".log.txt";
                var spewPath = Path.Combine(spewFolder, spewFilename);
                string openAlFilename = Assembly.GetCallingAssembly().GetName().Name + ".openallog.txt";
                var openalPath = Path.Combine(spewFolder, openAlFilename);
                if(!Debugger.IsAttached)
                    Environment.SetEnvironmentVariable("ALSOFT_LOGFILE", openalPath);
                if (FLLog.CreateSpewFile(spewPath)) errorMessage += "\n" + spewPath;
                else errorMessage += "\n(Log file could not be created).";
            }
#if !DEBUG
            var domain = AppDomain.CurrentDomain;
            domain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                var ex = (Exception)(e.ExceptionObject);

                CrashWindow.Run("Uh-oh!", errorMessage,
                FormatException(ex).ToString());
            };
            try
            {
#endif
            if (!Platform.CheckDependencies()) return;
            action();
#if !DEBUG
            }
            catch (Exception ex)
            {
                try { onCrash?.Invoke(); } catch { }
                CrashWindow.Run("Uh-oh!", errorMessage, FormatException(ex).ToString());
            }

#endif
        }

        static StringBuilder FormatException(Exception ex, StringBuilder builder = default, int j = 0)
        {
            bool addVersion = j == 0;
            builder ??= new StringBuilder();
            builder.AppendLine(ex.Message);
            builder.AppendLine(ex.StackTrace);
            j++;
            if (j > 100) {
                builder.AppendLine("-- EXCEPTION OVERFLOW --");
                return builder;
            }
            if (ex is AggregateException ag)
            {
                for (int i = 0; i < ag.InnerExceptions.Count; i++)
                {
                    builder.AppendLine($"Inner Exception #{i + 1}:");
                    FormatException(ag.InnerExceptions[i], builder, j);
                }
            }
            else
            {
                if (ex.InnerException != null)
                {
                    builder.AppendLine("Inner Exception: ");
                    FormatException(ex.InnerException, builder, j);
                }
            }
            if (addVersion)
            {
                builder.AppendLine()
                    .AppendLine($"Librelancer Version: {Platform.GetInformationalVersion<Platforms.IPlatform>()}");
            }
            return builder;
        }
    }
}
