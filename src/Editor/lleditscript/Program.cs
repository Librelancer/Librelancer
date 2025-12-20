// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.Data.Schema;
using LibreLancer.Options;
using Microsoft.CodeAnalysis;

namespace lleditscript
{
    public class Globals //Must be public or compiler error in script
    {
        public string[] Arguments;

        public Globals(string[] args, string scriptName)
        {
            this.Arguments = args;
            this.scriptName = scriptName;
        }

        private string scriptName;
        private string usage = "";

        public void ScriptUsage(string usage)
        {
            this.usage = usage;
        }

        private OptionSet os = new OptionSet();

        public void PrintMessages<T>(EditResult<T> result)
        {
            foreach(var m in result.Messages)
                Console.WriteLine($"{m.Kind}: {m.Message}");
        }

        public void FlagOption(string prototype, string description, Action<bool> action)
        {
            os.Add(prototype, description, f => action(f != null));
        }

        public void StringOption(string prototype, string description, Action<string> action)
        {
            os.Add(prototype, description, action);
        }

        public void IntegerOption(string prototype, string description, Action<int> action)
        {
            os.Add(prototype, description, action);
        }

        void PrintUsage()
        {
            if (!string.IsNullOrEmpty(usage))
                Console.WriteLine($"Usage: lleditscript {scriptName} {usage}");
            else {
                Console.WriteLine($"Usage: lleditscript {scriptName} [options] [arguments]");
            }
            os.WriteOptionDescriptions(Console.Out);
        }

        public string[] ParseArguments(int minArguments = 0)
        {
            List<string> extra;
            try
            {
                extra = os.Parse(Arguments);
                if (extra.Count < minArguments) {
                    PrintUsage();
                    Environment.Exit(0);
                }
                return extra.ToArray();
            }
            catch (OptionException e) {
                Console.Write ($"lleditscript {scriptName}: ");
                Console.WriteLine (e.Message);
                Environment.Exit(1);
                return Array.Empty<string>();
            }
        }

        public void AssertFileExists(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.Error.WriteLine($"Could not find file '{filename}'");
                Environment.Exit(2);
            }
        }

        public void AssertFilesExist(IEnumerable<string> filenames)
        {
            foreach (var f in filenames)
                AssertFileExists(f);
        }

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
            "LibreLancer.ContentEdit.Model",
            "LibreLancer"
        };

        static string AssemblyEditorScriptPath()
        {
            return Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "editorscripts");
        }

        static string ModuleEditorScriptPath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            var basePath = Path.GetDirectoryName(processModule?.FileName);
            return Path.Combine(basePath, "editorscripts");
        }

        static string SearchFile(params string[] files) =>
            files.FirstOrDefault(File.Exists, files[^1]);

        static Script<object> CompileScript(string filePath, string scriptText)
        {
            var opts = ScriptOptions.Default.WithReferences(
                    typeof(Vector3).Assembly, typeof(FreelancerData).Assembly,
                    typeof(FreelancerGame).Assembly, typeof(string).Assembly,
                    typeof(EditableUtf).Assembly, typeof(Game).Assembly)
                .WithImports(Namespaces).WithAllowUnsafe(true).WithFilePath(filePath);
            var script = CSharpScript.Create(scriptText, opts, typeof(Globals));
            var result = script.Compile();
            foreach (var diag in result)
            {
                Console.WriteLine(diag);
            }
            if (result.Any(x => x.Severity == DiagnosticSeverity.Error))
                return null;
            return script;
        }

        static int RunAssembly(Assembly scriptAssembly, Globals globals)
        {
            var type = scriptAssembly.GetType("Submission#0");
            var factory = type.GetMethod("<Factory>");
            var submissionArray = new object[2];
            submissionArray[0] = globals;
            var tk = (Task<object>)factory.Invoke(null, new object[] { submissionArray });
            tk.Wait();
            if (tk.Exception != null)
            {
                LogException(tk.Exception);
                return 1;
            }
            return 0;
        }

        static string ReadScript(string filePath)
        {
            var scriptText = File.ReadAllText(filePath);
            if (scriptText.StartsWith("#!"))
            {
                scriptText = "#line 1\n" + scriptText.Substring(scriptText.IndexOf('\n'));
            }
            return scriptText;
        }


        static int Main(string[] args)
        {
            AppHandler.ConsoleInit(true);

            bool argsStdin = false;
            bool modulePath = false;
            bool testCompile = false;
            bool compile = false;
            int argStart;

            for (argStart = 0; argStart < args.Length; argStart++)
            {
                var a = args[argStart];
                if (a == "--args-stdin") {
                    argsStdin = true;
                }
                else if (a == "-m") {
                    modulePath = true;
                }
                else if (a == "--test-compile") {
                    testCompile = true;
                }
                else if (a == "--compile" || a == "-c") {
                    compile = true;
                }
                else if (a == "--") {
                    argStart++;
                    break;
                }
                else {
                    break;
                }
            }

            if (args.Length <= argStart)
            {
                Console.Error.WriteLine("Usage: lleditscript [--args-stdin] [-m] script.cs [arguments]");
                return 0;
            }

            var filePath = args[argStart];
            if (modulePath)
            {
                filePath = SearchFile(
                    Path.Combine(AssemblyEditorScriptPath(), filePath),
                    Path.Combine(AssemblyEditorScriptPath(), filePath) + ".cs-script",
                    Path.Combine(ModuleEditorScriptPath(), filePath),
                    Path.Combine(ModuleEditorScriptPath(), filePath) + ".cs-script",
                    filePath,
                    filePath + ".cs-script"
                );
            }

            string[] scriptArguments;
            if (argsStdin)
            {
                var input_args = new List<string>();
                string line;
                while ((line = Console.ReadLine()) != null)
                {
                    input_args.Add(line);
                }
                scriptArguments = input_args.ToArray();
            } else
            {
                scriptArguments = args.Skip(argStart + 1).ToArray();
            }

            string scriptText = null;
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine("Script file {1}'{0}' not found", filePath, modulePath ? "(or module) " : "");
                return 2;
            }

            if (IsPEFile(filePath))
            {
                if (compile || testCompile)
                {
                    Console.Error.WriteLine($"Cannot compile binary file '{filePath}'");
                    return 2;
                }
                var scriptAssembly = Assembly.Load(File.ReadAllBytes(filePath));
                return RunAssembly(scriptAssembly, new Globals(scriptArguments,
                    modulePath ? $"-m {args[argStart]}" : args[argStart]));
            }

            scriptText = ReadScript(filePath);

            try
            {
                var globals = new Globals(scriptArguments,
                    modulePath ? $"-m {args[argStart]}" : args[argStart]);
                if (testCompile)
                {
                    Console.WriteLine($"Compiling '{filePath}'");
                    if (CompileScript(filePath, scriptText) == null)
                        return 1;
                    foreach (var f in scriptArguments)
                    {
                        Console.WriteLine($"Compiling '{f}'");
                        if (!File.Exists(filePath))
                        {
                            Console.Error.WriteLine("Script file '{0}' not found", filePath);
                            return 2;
                        }
                        if (CompileScript(f, ReadScript(f)) == null)
                            return 1;
                    }
                    return 0;
                }
                else if (compile)
                {
                    if (args.Length <= argStart + 1)
                    {
                        Console.WriteLine("Output file must be specified for compilation");
                        return 2;
                    }
                    var result = CompileScript(filePath, scriptText);
                    if (result == null)
                        return 1;
                    var compilation = result.GetCompilation();
                    using var output = File.Create(args[argStart + 1]);
                    var emitResult = compilation.Emit(output);
                    if (!emitResult.Success) {
                        Console.WriteLine("Emit failed");
                        return 1;
                    }
                }
                else
                {
                    var p = Cache.GetCacheItem(scriptText);
                    if (p == null)
                    {
                        var sc = CompileScript(filePath, scriptText);
                        var compilation = sc.GetCompilation();
                        var output = new MemoryStream();
                        using(var comp = new ZstdSharp.CompressionStream(output, 9))
                        {
                            compilation.Emit(comp);
                        }
                        Cache.WriteCacheItem(scriptText, output.ToArray());
                        var tk = sc.RunAsync(globals);
                        tk!.Wait();
                        if (tk.Result.Exception != null)
                        {
                            LogException(tk.Result.Exception);
                            return 1;
                        }
                    }
                    else
                    {
                        MemoryStream asm = new MemoryStream();
                        {
                            using var input = new MemoryStream(p);
                            using var decomp = new ZstdSharp.DecompressionStream(input);
                            decomp.CopyTo(asm);
                        }
                        var loaded = Assembly.Load(asm.ToArray());
                        return RunAssembly(loaded, globals);
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e);
                return 1;
            }

            return 0;
        }

        static bool IsPEFile(string path)
        {
            using var stream = File.OpenRead(path);
            Span<byte> mz = stackalloc byte[2];
            stream.Read(mz);
            if (mz[0] != (byte)'M' || mz[1] != (byte)'Z')
                return false;
            return true;
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
