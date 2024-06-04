using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreLancer
{
    public static class Shell
    {
        public static bool HasCommand(string cmd)
        {
            var startInfo = new ProcessStartInfo("/bin/sh")
            {
                UseShellExecute = false,
                Arguments = $" -c \"command -v {cmd} >/dev/null 2>&1\""
            };
            using var p = Process.Start(startInfo);
            p.WaitForExit();
            return p.ExitCode == 0;
        }

        public static void TryKill(this Process p)
        {
            // ReSharper disable once EmptyGeneralCatchClause
            try { p.Kill(); } catch { }
        }

        public static string GetString(string cmd, string args, int timeout = 0)
        {
            try
            {
                var startInfo = new ProcessStartInfo(cmd)
                {
                    UseShellExecute = false,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(startInfo);
                if (timeout > 0)
                {
                    if (!p.WaitForExit(timeout))
                    {
                        FLLog.Warning("Shell", $"Timeout of {timeout}ms exceeded for: {cmd} {args}");
                        p.TryKill();
                        return "";
                    }
                }
                else
                    p.WaitForExit();
                return p.StandardOutput.ReadToEnd().Trim();
            }
            catch (Exception e)
            {
                FLLog.Warning("Shell", $"Error running {cmd} {args}");
                return "";
            }
        }

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

        public static void OpenCommand(string path)
        {
            if(Platform.RunningOS == OS.Windows)
            {
                Process.Start(new ProcessStartInfo(path) {UseShellExecute = true});
            } else if (Platform.RunningOS == OS.Mac) {
                Process.Start("open", string.Format("\"{0}\"", path));
            } else if (Platform.RunningOS == OS.Linux) {
                Process.Start("xdg-open", string.Format("\"{0}\"", path));
            }
        }
    }
}
