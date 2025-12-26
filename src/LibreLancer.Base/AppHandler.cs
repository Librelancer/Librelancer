using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using LibreLancer.Dialogs;

namespace LibreLancer;

public static class AppHandler
{
    [DllImport("kernel32.dll")]
    private static extern bool SetDllDirectory(string directory);

    public const string ProjectName = "Librelancer";

    private static void LogPlatform()
    {
        FLLog.Info("Platform", Platform.OSDescription);
        FLLog.Info("Available Threads", Environment.ProcessorCount.ToString());
    }

    public static void ConsoleInit(bool silent = false)
    {
        if (!silent)
        {
            LogPlatform();
        }

        if (Platform.RunningOS != OS.Windows)
        {
            return;
        }

        var binDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location) ?? "";
        var fullPath = Path.Combine(binDir, IntPtr.Size == 8 ? "x64" : "x86");
        SetDllDirectory(fullPath);
    }

    private static string LogsFolder()
    {
        if (Platform.RunningOS != OS.Linux)
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        var statePath = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
        if (!string.IsNullOrEmpty(statePath))
        {
            return statePath;
        }

        statePath = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(statePath))
        {
            return "./data";
        }

        return statePath + "/.local/state";
    }

    private static bool FileOk(string file)
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

    public static void Run(Action action, Action? onCrash = null)
    {
        var errorMessage = $"{ProjectName} has crashed. See the log for more information.";
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ALSOFT_LOGLEVEL")))
        {
            Environment.SetEnvironmentVariable("ALSOFT_LOGLEVEL", "2");
        }

        if (Platform.RunningOS == OS.Windows)
        {
            var binDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location) ?? "";
            var fullPath = Path.Combine(binDir, IntPtr.Size == 8 ? "x64" : "x86");
            SetDllDirectory(fullPath);
        }

        //Setup Spew
        var spewFolder = Path.Combine(LogsFolder(), ProjectName, "logs");
        if (!Directory.Exists(spewFolder))
        {
            Directory.CreateDirectory(spewFolder);
        }

        var spewBase = Assembly.GetCallingAssembly().GetName().Name + ".log";
        var spewSuffix = ".txt";

        string? spewPath = null;
        var fileCounter = 0;
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
            if (!Platform.CheckDependencies())
            {
                return;
            }

            action();
#if !DEBUG
        }
        catch (Exception ex)
        {
            try { onCrash?.Invoke(); }
            catch
            {
                // ignored
            }

            CrashWindow.Run("Uh-oh!", errorMessage, FormatException(ex).ToString());
        }

#endif
    }

    private static StringBuilder FormatException(Exception ex, StringBuilder? builder = null, int j = 0)
    {
        var addVersion = j == 0;
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
            for (var i = 0; i < ag.InnerExceptions.Count; i++)
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
