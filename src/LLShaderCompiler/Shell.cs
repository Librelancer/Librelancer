using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LLShaderCompiler;

static class Shell
{
    public static async Task<int> Run(string cmd, params string[] arguments)
    {
        var args = string.Join(' ', arguments.Select(Quote));
        var startInfo = new ProcessStartInfo(cmd)
        {
            UseShellExecute = false,
            Arguments = args
        };
        var p = Process.Start(startInfo);
        if (p == null)
        {
            return int.MinValue;
        }
        await p.WaitForExitAsync();
        return p.ExitCode;
    }

    public static async Task<string> RunString(string cmd, params string[] arguments)
    {
        var args = string.Join(' ', arguments.Select(Quote));
        var startInfo = new ProcessStartInfo(cmd)
        {
            UseShellExecute = false,
            Arguments = args,
            RedirectStandardOutput = true
        };
        var p = Process.Start(startInfo);
        if (p == null)
        {
            throw new Exception($"Fail running on {cmd} {args}");
        }
        await p.WaitForExitAsync();
        if (p.ExitCode != 0)
            throw new Exception($"Fail running on {cmd} {args}");
        return await p.StandardOutput.ReadToEndAsync();
    }


    // https://learn.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way
    private static readonly char[] escapeChars = ['\t', '\n', ' ', '\v', '\"'];
    static string Quote(string s)
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

    public static string? Which(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetFullPathFromWindows(command);
        }
        else
        {
            return UnixHasCommand(command) ? "command" : null;
        }
    }

    static bool UnixHasCommand(string cmd)
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


    static string? GetFullPathFromWindows(string exeName)
    {
        if (exeName.Length >= MAX_PATH)
            throw new ArgumentException($"The executable name '{exeName}' must have less than {MAX_PATH} characters.",
                nameof(exeName));

        StringBuilder sb = new StringBuilder(exeName, MAX_PATH);
        return PathFindOnPath(sb, null) ? sb.ToString() : null;
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[]? ppszOtherDirs);
    private const int MAX_PATH = 260;
}
