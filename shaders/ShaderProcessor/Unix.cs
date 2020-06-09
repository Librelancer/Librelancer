// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Diagnostics;
using System.IO;

namespace ShaderProcessor
{
    public class Unix
    {
        public static string ResolveExecPath(string exe)
        {
            if (File.Exists(exe)) return Path.GetFullPath(exe);
            return FindCommand(exe);
        }
        static string FindCommand(string cmd)
        {
            var startInfo = new ProcessStartInfo("/bin/sh")
            {
                UseShellExecute = false,
                Arguments = $" -c \"command -v {cmd}\"",
                RedirectStandardOutput =  true,
                RedirectStandardError = true
            };
            var p = Process.Start(startInfo);
            p.WaitForExit();
            return p.ExitCode == 0 ? (p.StandardOutput.ReadToEnd().Trim()) : null;
        }
    }
}