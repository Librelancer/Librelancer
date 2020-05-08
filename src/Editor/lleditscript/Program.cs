// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Numerics;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data;

namespace lleditscript
{
    public class Globals //Must be public or compiler error in script
    {
        public string[] Arguments;
    }
    
    class Program
    {
        private static readonly string[] Namespaces =
        {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Numerics",
            "LibreLancer.ContentEdit",
            "LibreLancer"
        };
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: lleditscript script.cs [arguments]");
                return 0;
            }
            var filePath = args[0];
            string scriptText = null;
            if (!File.Exists(args[0]))
            {
                Console.Error.WriteLine("Script file '{0}' not found", args[0]);
                return 2;
            }
            
            scriptText = File.ReadAllText(filePath);

            if (scriptText.StartsWith("#!"))
            {
                scriptText = "#line 1\n" + scriptText.Substring(scriptText.IndexOf('\n'));
            }
            
            try
            {
                var opts = ScriptOptions.Default.WithReferences(
                    typeof(Vector3).Assembly, typeof(FreelancerData).Assembly,
                    typeof(FreelancerGame).Assembly, typeof(string).Assembly,
                    typeof(EditableUtf).Assembly, typeof(Game).Assembly)
                    .WithImports(Namespaces).WithAllowUnsafe(true).WithFilePath(filePath);
                var globals = new Globals {Arguments = args.Skip(1).ToArray()};
                var script = CSharpScript.Create(scriptText, opts, typeof(Globals));
                var tk = script.RunAsync(globals);
                tk.Wait();
                if (tk.Result.Exception != null)
                {
                    LogException(tk.Result.Exception);
                    return 1;
                }
            }
            catch (Exception e)
            {
                LogException(e);
                return 1;
            }
        
            return 0;
        }

        static void LogException(Exception ex)
        {
            Console.Error.WriteLine("EXCEPTION: {0}", ex.GetType());
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
            var inner = ex.InnerException;
            int level = 0;
            while (inner != null && level++ < 4)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Inner Exception: {0}", inner.GetType());
                Console.Error.WriteLine(inner.Message);
                Console.Error.WriteLine(inner.StackTrace);
                inner = inner.InnerException;
            }
        }
    }
}