using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using static BuildLL.Runtime;

namespace BuildLL
{
    public class CMake
    {
        public static void Run(string directory, CMakeSettings settings = null)
        {
            var argBuilder = new StringBuilder();
            var workingDir = Path.GetFullPath(settings?.OutputPath ?? directory);
            argBuilder.Append("-B ").AppendQuoted(workingDir);
            argBuilder.Append(" -S ").AppendQuoted(Path.GetFullPath(directory));
            Directory.CreateDirectory(workingDir);
            if (settings?.Platform != null)
            {
                argBuilder.Append(" -A ").AppendQuoted(settings.Platform);
            }
            if (settings?.Generator != null)
            {
                argBuilder.Append(" -G ").AppendQuoted(settings.Generator);
            }
            if (settings?.Options != null)
            {
                foreach (var o in settings.Options)
                    argBuilder.Append(" ").Append(o);
            }
            if (!string.IsNullOrEmpty(settings?.BuildType))
            {
                argBuilder.Append(" -DCMAKE_BUILD_TYPE=").Append(settings.BuildType).Append(" ");
            }
            string cmakePath = "cmake";
            if (IsWindows)
            {
                cmakePath = FindExeWin32("cmake.exe",
                    "%programfiles%\\cmake\\bin\\cmake.exe",
                    "%programfiles(x86)%\\cmake\\bin\\cmake.exe");
                if (string.IsNullOrEmpty(cmakePath))
                    throw new Exception("Unable to find a cmake installation. Try adding CMake to your PATH");
            }
            RunCommand(cmakePath, argBuilder.ToString(), workingDir);
        }
    }

    public class CMakeSettings
    {
        public string OutputPath { get; set; }
        public string Generator { get; set; }
        public string Platform { get; set; }
        public string[] Options { get; set; }
        public string BuildType { get; set; }
    }
}
