using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreLancer;

public static class Shell
{
    public static bool HasCommand(string cmd)
    {
        var startInfo = new ProcessStartInfo("/bin/sh")
        {
            UseShellExecute = false,
            Arguments = $" -c \"command -v {cmd} >/dev/null 2>&1\""
        };
        using var p = Process.Start(startInfo)!;
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
            using var p = Process.Start(startInfo)!;
            if (timeout > 0)
            {
                if (p.WaitForExit(timeout))
                {
                    return p.StandardOutput.ReadToEnd().Trim();
                }

                FLLog.Warning("Shell", $"Timeout of {timeout}ms exceeded for: {cmd} {args}");
                p.TryKill();
                return "";
            }

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
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return s.IndexOfAny(escapeChars) switch
            {
                -1 when s.IndexOf('\\') == -1 => s,
                _ => $"\"{s.Replace("\\", @"\\").Replace("\"", "\\\"")}\""
            };
        }

        if (s.IndexOfAny(escapeChars) == -1)
        {
            return s;
        }

        var b = new StringBuilder();
        b.Append('\"');

        for (var i = 0; i < s.Length; i++)
        {
            var numBackslashes = 0;

            while (i < s.Length && s[i] == '\\')
            {
                i++;
                numBackslashes++;
            }

            if (i == s.Length)
            {
                for (var j = 0; j < numBackslashes * 2; j++)
                {
                    b.Append('\\');
                }
            }
            else if (s[i] == '\"')
            {
                for (var j = 0; j < numBackslashes * 2; j++)
                {
                    b.Append('\\');
                }

                b.Append('\"');
            }
            else
            {
                for (var j = 0; j < numBackslashes; j++)
                {
                    b.Append('\\');
                }

                b.Append(s[i]);
            }
        }

        b.Append('\"');
        return b.ToString();

    }

    public static void OpenCommand(string path)
    {
        switch (Platform.RunningOS)
        {
            case OS.Windows:
                Process.Start(new ProcessStartInfo(path) {UseShellExecute = true});
                break;
            case OS.Mac:
                Process.Start("open", $"\"{path}\"");
                break;
            case OS.Linux:
                Process.Start("xdg-open", $"\"{path}\"");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
