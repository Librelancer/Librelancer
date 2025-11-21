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
            if (!silent)
                LogPlatform();
            if (Platform.RunningOS == OS.Windows)
            {
                string bindir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                var fullpath = Path.Combine(bindir, IntPtr.Size == 8 ? "x64" : "x86");
                SetDllDirectory(fullpath);
            }
        }

        static string LogsFolder()
        {
            if (Platform.RunningOS != OS.Linux)
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string statePath = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
            if (!string.IsNullOrEmpty(statePath))
                return statePath;
            statePath = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(statePath))
                return "./data";
            return statePath + "/.local/state";
        }

        static bool FileOk(string file)
        {
            if (!File.Exists(file))
                return true;
            try
            {
                var stream = File.OpenWrite(file);
                stream.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Run(Action action, Action onCrash = null)
        {
            string errorMessage = $"{ProjectName} has crashed. See the log for more information.";
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ALSOFT_LOGLEVEL")))
            {
                Environment.SetEnvironmentVariable("ALSOFT_LOGLEVEL", "2");
            }

            if (Platform.RunningOS == OS.Windows)
            {
                string bindir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                var fullpath = Path.Combine(bindir, IntPtr.Size == 8 ? "x64" : "x86");
                SetDllDirectory(fullpath);
            }

            //Setup Spew
            var spewFolder = Path.Combine(LogsFolder(), ProjectName, "logs");
            if (!Directory.Exists(spewFolder)) Directory.CreateDirectory(spewFolder);
            string spewBase = Assembly.GetCallingAssembly().GetName().Name + ".log";
            string spewSuffix = ".txt";
            string spewPath = null;
            int fileCounter = 0;
            do
            {
                var tryPath = Path.Combine(spewFolder, spewBase + spewSuffix);
                if (FileOk(tryPath))
                {
                    spewPath = tryPath;
                    break;
                }

                if (fileCounter > 5)
                {
                    break;
                }

                spewSuffix = $".{fileCounter++}.txt";
            } while (true);

            var openalPath = Path.Combine(spewFolder, spewBase + ".allog" + spewSuffix);
            if (string.IsNullOrWhiteSpace("ALSOFT_LOGFILE"))
            {
                Environment.SetEnvironmentVariable("ALSOFT_LOGFILE", openalPath);
            }

            if (spewPath != null && FLLog.CreateSpewFile(spewPath))
            {
                errorMessage += "\n" + spewPath;
            }
            else
            {
                errorMessage += "\n(Log file could not be created).";
                spewPath = null;
            }

            LogPlatform();
            if (spewPath != null)
            {
                FLLog.Info("Log Path", spewPath);
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
            if (j > 100)
            {
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
