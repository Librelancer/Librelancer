// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Options;

namespace ShaderProcessor
{
    public class CodeGenOptions
    {
        public string Namespace = "Namespace";
        public string ShaderType = "Shader";
        public string ShaderCompileMethod = "Shader.Compile";
        public string[] Imports;
        public bool Public = true;
        public bool Log = true;
        public string LogMethod = "ShaderLogger.Log";
        public string OutputDirectory;
        public bool Brotli = false;
    }
    
    class Program
    {
        static int Main(string[] args)
        {
            bool shouldShowHelp = false;
            var codeOpts = new CodeGenOptions();
            string glslValidator = "glslangValidator";
            var imports = new List<string>();
            var options = new OptionSet
            {
                { "o|output=", "output directory", n => codeOpts.OutputDirectory = n},
                { "g|glslangValidator=", "glslangValidator path", g => glslValidator = g},
                { "b|brotli", "compress with brotli (.NET Core only)", b => codeOpts.Brotli = b != null },
                { "l|log", "generate logging code", l => codeOpts.Log = l != null },
                { "x|logmethod=", "logging method name", x => codeOpts.LogMethod = x.Trim() },
                { "t|type=", "shader type name", t => codeOpts.ShaderType = t.Trim() },
                { "i|import=", "import namespace", i => imports.Add(i.Trim()) },
                { "n|namespace=", "generated code namespace", n => codeOpts.Namespace = n.Trim() },
                { "c|compilemethod=", "shader compile method name", c => codeOpts.ShaderCompileMethod = c.Trim() },
                { "p|private", "generate internal classes", p => codeOpts.Public = p == null },
                { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
            };
            List<string> input = null;
            try
            {
                input = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("shaderprocessor: ");
                Console.WriteLine(e.Message);
                Console.WriteLine ("Try `shaderprocessor --help' for more information.");
                return 1;
            }
            if (shouldShowHelp || input?.Count < 1)
            {
                WriteHelp(options);
                return 0;
            }
            if (string.IsNullOrEmpty(codeOpts.OutputDirectory))
            {
                Console.WriteLine("Output directory must be specified.");
                Console.WriteLine ("Try `shaderprocessor --help' for more information.");
                return 2;
            }

            foreach (var i in input)
            {
                if (!File.Exists(i))
                {
                    Console.Error.WriteLine($"File does not exist {i}");
                    return 2;
                }
            }

            codeOpts.Imports = imports.ToArray();
            List<EffectFile> effects = new List<EffectFile>();
            foreach (var i in input)
            {
                var fx = EffectFile.Read(i);
                if (fx == null)
                {
                    Console.Error.WriteLine($"Error reading file {i}");
                    return 1;
                }
                effects.Add(fx);
            }
            //Don't rely on filesystem sorting, makes merges less crappy
            effects.Sort((x,y) => String.Compare(x.Name, y.Name, StringComparison.Ordinal));

            Glslang.ToolPath = GetPath(glslValidator);
            if (Glslang.ToolPath != null)
            {
                foreach (var fx in effects)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        //Validate for 150 and 430
                        var ver = (i == 1) ? "430" : "150";
                        var vs = (i == 1) ? new[] {"VERTEX_SHADER", "FEATURES430" } : new[] {"VERTEX_SHADER"};
                        var fs = (i == 1) ? new[] {"FRAGMENT_SHADER", "FEATURES430" } : new[] {"FRAGMENT_SHADER"};
                        //Default defines
                        //VERTEX_SHADER
                        //FRAGMENT_SHADER
                        if (!Glslang.ValidateShader($"{fx.Name} vertex shader", "vert",
                            InsertDefine(fx.VertexSource, ver, vs)))
                            return 1;
                        if (!Glslang.ValidateShader($"{fx.Name} fragment shader", "frag",
                            InsertDefine(fx.FragmentSource, ver, fs)))
                            return 1;
                        //Validate syntax for all combinations of preprocessor defs
                        foreach (var featureSet in FeatureHelper.Permute(null, fx.Features))
                        {
                            var vdefs = vs.Concat(featureSet);
                            var fdefs = fs.Concat(featureSet);
                            var vsName = $"{fx.Name} vertex shader ({string.Join(" | ", featureSet)})";
                            var fsName = $"{fx.Name} fragment shader ({string.Join(" | ", featureSet)}";
                            if (!Glslang.ValidateShader(vsName, "vert", InsertDefine(fx.VertexSource, ver, vdefs)))
                                return 1;
                            if (!Glslang.ValidateShader(fsName, "frag", InsertDefine(fx.FragmentSource, ver, fdefs)))
                                return 1;
                        }
                    }
                    fx.VertexSource = "#define VERTEX_SHADER\n#line 1\n" + fx.VertexSource;
                    fx.FragmentSource = "#define FRAGMENT_SHADER\n#line 1\n" + fx.FragmentSource;
                }
            }
            else
            {
                Console.Error.WriteLine("WARNING: Glslang not found. Skipping validation.");
            }
            Generate(codeOpts, effects);
            return 0;
        }

        static string InsertDefine(string shader, string version, IEnumerable<string> defs)
        {
            var builder = new StringBuilder();
            builder.Append("#version ").AppendLine(version);
            foreach (var def in defs)
                builder.Append("#define ").AppendLine(def);
            builder.AppendLine("#line 1");
            builder.Append(shader);
            return builder.ToString();
        }
        static void WriteHelp(OptionSet options)
        {
            Console.WriteLine("Usage: shaderprocessor [OPTIONS]+ -o output inputs");
            Console.WriteLine("Creates C# source from glsl effects");
            Console.WriteLine();
            Console.WriteLine("Options: ");
            options.WriteOptionDescriptions(Console.Out);
        }
        static string GetPath(string exe)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Win32.ResolveExecPath(exe);
            return Unix.ResolveExecPath(exe);
        }
        
        static void Generate(CodeGenOptions options, IEnumerable<EffectFile> inputs)
        {
            if (!Directory.Exists(options.OutputDirectory))
                Directory.CreateDirectory(options.OutputDirectory);
            List<string> vals = new List<string>();
            foreach (var input in inputs)
            {
                foreach(var f in input.Features)
                    if (!vals.Contains(f))
                        vals.Add(f);
            }
            vals.Sort();
            Dictionary<string,int> enumVals = new Dictionary<string, int>();
            for (int i = 0; i < vals.Count; i++)
            {
                enumVals.Add(vals[i], 1 << i);
            }
            foreach (var input in inputs)
            {
                File.WriteAllText(
                    Path.Combine(options.OutputDirectory, $"{input.Name}.cs"),
                    CodeGenerator.Generate(options, input, enumVals),
                    Encoding.UTF8
                );
            }
            File.WriteAllText(
                Path.Combine(options.OutputDirectory, "ShaderFeatures.cs"),
                CodeGenerator.CreateEnum(options, vals),
                Encoding.UTF8
            );
           
            File.WriteAllText(
                Path.Combine(options.OutputDirectory, "ShCompHelper.cs"), 
                DecompressTemplate.Text.Replace("{NAMESPACE}", options.Namespace).
                    Replace("{COMPRESS}", options.Brotli ? "Brotli" : "Deflate"),
                Encoding.UTF8
            );
            File.WriteAllText(
                Path.Combine(options.OutputDirectory, "AllShaders.cs"),
                CodeGenerator.AllShaders(options, inputs),
                Encoding.UTF8
            );
        }
    }
}