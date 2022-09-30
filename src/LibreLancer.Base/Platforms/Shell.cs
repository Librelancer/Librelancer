using System;
using System.Diagnostics;

namespace LibreLancer
{
    static class Shell
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

        public static string GetString(string cmd, string args)
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
                p.WaitForExit();
                return p.StandardOutput.ReadToEnd().Trim();
            }
            catch (Exception e)
            {
                FLLog.Warning("Shell", $"Error running {cmd} {args}");
                return "";
            }
        }
    }
}