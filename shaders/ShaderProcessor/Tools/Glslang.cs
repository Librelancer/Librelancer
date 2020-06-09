// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Diagnostics;
using System.IO;

namespace ShaderProcessor
{
    public static class Glslang
    {
        public static string ToolPath = "glslangValidator";
        
        public static bool ValidateShader(string name, string stage, string source)
        {
            var (exitcode, stdout) = InvokeGlslang("-l -q", stage, source);
            if (exitcode != 0)
            {
                PrintErrors(stdout);
                Console.Error.WriteLine("Error compiling {0}", name);
                return false;
            }
            return true;
        }
        
        static void PrintErrors(string stdout)
        {
            var reader = new StringReader(stdout);
            string ln = null;
            while ((ln = reader.ReadLine()) != null)
            {
                ln = ln.Trim();
                if(ln.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
                    Console.Error.WriteLine(ln);
            }
        }
        static (int, string) InvokeGlslang(string arguments, string stage, string source)
        {
            var args = $"{arguments} --stdin -S {stage}";
            var psi = new ProcessStartInfo(ToolPath, args);
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            var p = Process.Start(psi);
            p.StandardInput.Write(source);
            p.StandardInput.Close();
            p.WaitForExit();
            Console.Error.Write(p.StandardError.ReadToEnd());
            var sinput = p.StandardOutput.ReadToEnd();
            return (p.ExitCode, sinput);
        }
    }
}