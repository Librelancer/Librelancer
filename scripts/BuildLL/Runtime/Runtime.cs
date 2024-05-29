using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using Bullseye;
using static Bullseye.Targets;
using Process = System.Diagnostics.Process;

namespace BuildLL
{
    public static class Runtime
    {
        // https://learn.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way
        private static readonly char[] escapeChars = ['\t', '\n', ' ', '\v', '\"'];
        public static string Quote(string s)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (s.IndexOfAny(escapeChars) == -1)
                    return s;
                var b = new StringBuilder();
                b.Append('\"');
                for (int i = 0; i < s.Length; i++)
                {
                    int numBackslashes = 0;
                    while (i < s.Length && s[i] == '\\') {
                        i++;
                        numBackslashes++;
                    }
                    if (i == s.Length)
                    {
                        for (int j = 0; j < numBackslashes * 2; j++)
                            b.Append('\\');
                    }
                    else if (s[i] == '\"')
                    {
                        for (int j = 0; j < numBackslashes * 2; j++)
                            b.Append('\\');
                        b.Append('\"');
                    }
                    else
                    {
                        for (int j = 0; j < numBackslashes; j++)
                            b.Append('\\');
                        b.Append(s[i]);
                    }
                }
                b.Append('\"');
                return b.ToString();
            }
            else
            {
                if (s.IndexOfAny(escapeChars) == -1 &&
                    s.IndexOf('\\') == -1)
                    return s;
                return $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
            }
        }

        public static StringBuilder AppendQuoted(this StringBuilder builder, string s)
        {
            return builder.Append(Quote(s));
        }

        public static bool TryGetEnv(string variable, out string value)
        {
            value = Environment.GetEnvironmentVariable(variable);
            return !string.IsNullOrWhiteSpace(value);
        }

        public static void CopyFile(string src, string dst)
        {
            if (Directory.Exists(dst))
                dst = Path.Combine(dst, Path.GetFileName(src));
            File.Copy(src, dst, true);
        }

        public static void CopyDirContents(string source, string target, bool recursive = false, string searchPattern = "*")
        {
            CopyDirContents(new DirectoryInfo(source), new DirectoryInfo(target), recursive, searchPattern);
        }
        static void CopyDirContents (DirectoryInfo source, DirectoryInfo target, bool recursive, string searchPattern) {
            if (recursive)
            {
                foreach (DirectoryInfo dir in source.GetDirectories())
                    CopyDirContents(dir, target.CreateSubdirectory(dir.Name), true, searchPattern);
            }
            foreach (FileInfo file in source.GetFiles()) {
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }
        }

        public static void RmDir(string directory)
        {
            if(Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        public static void Cmd(string command)
        {
            var psi = new ProcessStartInfo("cmd", $"/c \"{command}\"");
            var process = Process.Start(psi);
            process.WaitForExit();
            if (process.ExitCode != 0) throw new Exception($"Command Failed: {command}");
        }

        public static string Bash(string command, bool print = true)
        {
            if (print && IsVerbose) Console.WriteLine(command);
            var psi = new ProcessStartInfo("/usr/bin/env", "bash");
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            var process = Process.Start(psi);
            process.StandardInput.Write(command);
            process.StandardInput.Close();
            var task = process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();
            task.Wait();
            if (process.ExitCode != 0) throw new Exception($"Command Failed: {command}");
            return task.Result.Trim();
        }

        public static void RunCommand(string cmd, string args, string workingDirectory = null)
        {
            if(IsVerbose) Console.WriteLine($"{cmd} {args}");
            workingDirectory ??= Environment.CurrentDirectory;
            var psi = new ProcessStartInfo(cmd, args);
            psi.WorkingDirectory = workingDirectory;
            var process = Process.Start(psi);
            process.WaitForExit();
            if (process.ExitCode != 0) throw new Exception($"Command Failed: {cmd} {args}");
        }

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsVerbose { get; private set; }

        private static CommandLineApplication _app;

        private static List<Action> setArgActions = new List<Action>();

        public static void StringArg(string option, Action<string> setVal, string description = "")
        {
            var a = _app.Option<string>(option, description, CommandOptionType.SingleValue);
            setArgActions.Add(() => {
                if (a.HasValue()) setVal(a.Value());
            });
        }
        public static void IntArg(string option, Action<int> setVal, string description = "")
        {
            var a = _app.Option<int>(option, description, CommandOptionType.SingleValue);
            setArgActions.Add(() =>
            {
                if (a.HasValue()) setVal(a.ParsedValue);
            });
        }

        public static void FlagArg(string option, Action setVal, string description = "")
        {
            var a = _app.Option(option, description, CommandOptionType.NoValue);
            setArgActions.Add(() =>
            {
                if (a.HasValue()) setVal();
            });
        }

        static int Main(string[] args)
        {
            using var app = new CommandLineApplication() {UsePagerForHelpText = false};
            app.Name = IsWindows ? "build.ps1" : "build.sh";
            _app = app;
            app.HelpOption();
            // add options
            Program.Options();
            //post build
            string postbuild = null;
            StringArg("--postbuild", x => postbuild = x, "Command to run after build");
            // translate from Bullseye to McMaster.Extensions.CommandLineUtils
            app.Argument("targets", "A list of targets to run or list. If not specified, the \"default\" target will be run, or all targets will be listed.", true);
            foreach (var option in Options.Definitions)
            {
                app.Option((option.ShortName != null ? $"{option.ShortName}|" : "") + option.LongName, option.Description, CommandOptionType.NoValue);
            }
            app.OnExecute(() =>
            {
                var targets = app.Arguments[0].Values;
                var options = new Options(Options.Definitions.Select(d => (d.LongName, app.Options.Single(o => "--" + o.LongName == d.LongName).HasValue())));
                IsVerbose = options.Verbose;
                foreach (var action in setArgActions) action();
                setArgActions = null;
                try
                {
                    Program.Targets();
                    if (WebHook.UseWebhook)
                    {
                        var message = $"Build started ({RuntimeInformation.OSDescription}).";
                        if (TryGetEnv("APPVEYOR_BUILD_NUMBER", out string jobNumber))
                            message += $" #{jobNumber}.";
                        WebHook.AppveyorDiscordWebhook(message);
                    }
                    var task = RunTargetsWithoutExitingAsync(targets, options);
                    task.Wait();
                    if (task.Exception != null)
                    {
                        OnError(task.Exception);
                        return 2;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(postbuild))
                        {
                            if(IsWindows)
                                Cmd(postbuild);
                            else
                                Bash(postbuild);
                        }
                        if (WebHook.UseWebhook)
                        {
                            var message = $"Build succeeded ({RuntimeInformation.OSDescription}).";
                            if (TryGetEnv("APPVEYOR_BUILD_NUMBER", out string jobNumber))
                                message += $" #{jobNumber}.";
                            WebHook.AppveyorDiscordWebhook(message);
                        }
                        return 0;
                    }
                }
                catch (Exception e)
                {
                    OnError(e);
                    return 2;
                }

            });
            return app.Execute(args);
        }

        static void OnError(Exception e)
        {
            Console.Error.WriteLine(e);
            if (WebHook.UseWebhook)
            {
                var message = $"Build failed ({RuntimeInformation.OSDescription}).";
                if (TryGetEnv("APPVEYOR_JOB_NUMBER", out string jobNumber))
                    message += $" #{jobNumber}.";
                message += "\n```\n";
                var msg = e.ToString();
                if(msg.Length > 500) msg = msg.Substring(0,500) + "\n...";
                message += msg + "\n```";
                WebHook.AppveyorDiscordWebhook(message);
            }
        }

        public static string FindExeWin32(string exe, params string[] extraPaths)
        {
            var p = GetFullPathFromWindows(exe);
            if (!string.IsNullOrWhiteSpace(p) && File.Exists(p)) return p;
            foreach (var path in extraPaths) {
                p = Environment.ExpandEnvironmentVariables(path);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        static string GetFullPathFromWindows(string exeName)
        {
            if (exeName.Length >= MAX_PATH)
                throw new ArgumentException($"The executable name '{exeName}' must have less than {MAX_PATH} characters.",
                    nameof(exeName));

            StringBuilder sb = new StringBuilder(exeName, MAX_PATH);
            return PathFindOnPath(sb, null) ? sb.ToString() : null;
        }

        //https://docs.microsoft.com/en-us/windows/desktop/api/shlwapi/nf-shlwapi-pathfindonpathw
        //https://www.pinvoke.net/default.aspx/shlwapi.PathFindOnPath
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[] ppszOtherDirs);
        private const int MAX_PATH = 260;
    }
}
